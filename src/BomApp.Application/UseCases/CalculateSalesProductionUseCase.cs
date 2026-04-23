using System.Text.Json;
using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.UseCases;

/// <summary>
/// Use case สำหรับคำนวณวัตถุดิบจากรายการขาย (หน้าจอ 2.5)
/// ใช้ร่วมกันทั้ง UI และ CLI — logic รวมที่เดียว ไม่ duplicate
/// </summary>
public class CalculateSalesProductionUseCase(
    IErpSalesOrderRepository erpSalesOrderRepository,
    IBomRepository bomRepository,
    IBomAssignmentRepository bomAssignmentRepository,
    IProductionOrderRepository productionOrderRepository)
    : ICalculateSalesProductionUseCase
{
    private const int MaxBomDepth = 10;

    /// <summary>
    /// คำนวณวัตถุดิบจากรายการขายตามช่วงวันที่
    /// Flow:
    /// 1. ดึงรายการขายจาก ERP (trans_flag=44, last_status=0)
    /// 2. lookup BOM จาก item_code ผ่าน BomAssignment
    /// 3. ตรวจสอบ BOM status == Active
    /// 4. คำนวณ material requirements (รวม multi-level BOM expansion)
    /// 5. รวมยอดวัตถุดิบเดียวกัน
    /// DryRun = true → คำนวณเท่านั้น ไม่ write DB
    /// </summary>
    public async Task<Result<ProductionResultDto>> CalculateAsync(
        CalculateSalesProductionRequest request,
        CancellationToken ct = default)
    {
        // Step 1: ดึงรายการขายจาก ERP
        var transactions = await erpSalesOrderRepository
            .GetSalesTransactionsByDateRangeAsync(request.DateFrom, request.DateTo, ct);

        if (transactions.Count == 0)
            return Result<ProductionResultDto>.Failure("ไม่มีรายการขายในช่วงวันที่ที่ระบุ");

        // Step 2: lookup BOM ผ่าน BomAssignment — batch query
        var allItemCodes = transactions.Select(t => t.ItemCode).Distinct().ToList();
        var assignmentMap = await bomAssignmentRepository.GetAssignedItemCodesAsync(allItemCodes, ct);

        var skippedItemCodes = allItemCodes
            .Where(code => !assignmentMap.ContainsKey(code))
            .ToArray();

        // group transactions ตาม ItemCode สำหรับสร้าง result items
        var transactionsByItem = transactions
            .Where(t => assignmentMap.ContainsKey(t.ItemCode))
            .GroupBy(t => t.ItemCode)
            .ToDictionary(g => g.Key, g => g.ToList());

        // aggregate materials ทั้งหมด (key = MaterialCode)
        var aggregatedMaterials = new Dictionary<string, (string Name, decimal Qty, string Unit)>();
        var resultItems = new List<ProductionResultItemDto>();

        foreach (var (itemCode, itemTransactions) in transactionsByItem)
        {
            var bomId = assignmentMap[itemCode];
            var bom = await bomRepository.GetByIdAsync(bomId, ct);

            // Step 3: ตรวจสอบ BOM Active
            if (bom is null || bom.Status != "Active")
                continue;

            // คำนวณยอดรวม qty_in_base_unit สำหรับสินค้านี้
            decimal totalQtyBase = itemTransactions.Sum(t => t.QtyInBaseUnit);
            decimal saleQty = itemTransactions.Sum(t => t.Qty);
            string saleUnit = itemTransactions.First().UnitCode;

            // Step 4: expand BOM และคำนวณ material requirements
            var itemMaterials = new Dictionary<string, (string Name, decimal Qty, string Unit)>();
            await ExpandBomAsync(bom, totalQtyBase, itemMaterials, new HashSet<Guid>(), depth: 0, ct);

            var materialReqs = itemMaterials
                .Select(kv => new MaterialRequirementDto(kv.Key, kv.Value.Name, kv.Value.Qty, kv.Value.Unit))
                .ToList();

            resultItems.Add(new ProductionResultItemDto(
                ItemCode: itemCode,
                ItemName: bom.ItemName,
                BomCode: bom.Code,
                SaleQty: saleQty,
                SaleUnit: saleUnit,
                QtyInBaseUnit: totalQtyBase,
                Materials: materialReqs));

            // Step 5: รวมยอดวัตถุดิบรวมทั้งหมด
            foreach (var (matCode, (matName, matQty, matUnit)) in itemMaterials)
            {
                if (aggregatedMaterials.TryGetValue(matCode, out var existing))
                    aggregatedMaterials[matCode] = (matName, existing.Qty + matQty, matUnit);
                else
                    aggregatedMaterials[matCode] = (matName, matQty, matUnit);
            }
        }

        var allMaterials = aggregatedMaterials
            .Select(kv => new MaterialRequirementDto(kv.Key, kv.Value.Name, kv.Value.Qty, kv.Value.Unit))
            .ToList();

        var result = new ProductionResultDto(
            Items: resultItems,
            Materials: allMaterials,
            SkippedItemCount: skippedItemCodes.Length,
            SkippedItemCodes: skippedItemCodes);

        return Result<ProductionResultDto>.Success(result);
    }

    /// <summary>
    /// บันทึก Production Orders จากผลการคำนวณ
    /// เรียก CalculateAsync ภายในก่อน จากนั้นสร้าง orders ตาม SaveMode
    /// </summary>
    public async Task<Result<IReadOnlyList<ProductionOrderDto>>> SaveAsync(
        CalculateSalesProductionRequest request,
        CancellationToken ct = default)
    {
        // คำนวณก่อนเสมอ
        var calcResult = await CalculateAsync(request, ct);
        if (!calcResult.IsSuccess)
            return Result<IReadOnlyList<ProductionOrderDto>>.Failure(calcResult.Error!);

        var productionResult = calcResult.Value!;

        // ดึงรายการขายอีกครั้งเพื่อสร้าง orders (ต้องการ doc grouping)
        var transactions = await erpSalesOrderRepository
            .GetSalesTransactionsByDateRangeAsync(request.DateFrom, request.DateTo, ct);

        var allItemCodes = transactions.Select(t => t.ItemCode).Distinct().ToList();
        var assignmentMap = await bomAssignmentRepository.GetAssignedItemCodesAsync(allItemCodes, ct);

        var eligibleTransactions = transactions
            .Where(t => assignmentMap.ContainsKey(t.ItemCode))
            .ToList();

        // ตรวจสอบ doc_no ที่ถูกบันทึกไปแล้ว
        var allDocNos = eligibleTransactions.Select(t => t.DocNo).Distinct().ToList();
        var alreadyProcessed = await productionOrderRepository.GetAlreadyProcessedDocNosAsync(allDocNos, ct);

        var createdOrders = new List<ProductionOrderDto>();

        if (request.Mode == SaveMode.Daily)
        {
            // รวม 1 เอกสารต่อวัน
            var byDate = eligibleTransactions.GroupBy(t => t.DocDate);
            foreach (var dateGroup in byDate)
            {
                var docNosForDate = dateGroup.Select(t => t.DocNo).Distinct().ToArray();
                var orders = await CreateProductionOrdersForGroupAsync(
                    dateGroup.ToList(),
                    docNosForDate,
                    sourceDocDateFrom: dateGroup.Key,
                    sourceDocDateTo: dateGroup.Key,
                    assignmentMap,
                    request,
                    ct);
                createdOrders.AddRange(orders);
            }
        }
        else // SaveMode.PerDocument
        {
            // แยกตามเลขเอกสารขาย
            var byDocNo = eligibleTransactions.GroupBy(t => t.DocNo);
            foreach (var docGroup in byDocNo)
            {
                var docDate = docGroup.First().DocDate;
                var orders = await CreateProductionOrdersForGroupAsync(
                    docGroup.ToList(),
                    [docGroup.Key],
                    sourceDocDateFrom: docDate,
                    sourceDocDateTo: docDate,
                    assignmentMap,
                    request,
                    ct);
                createdOrders.AddRange(orders);
            }
        }

        return Result<IReadOnlyList<ProductionOrderDto>>.Success(createdOrders);
    }

    /// <summary>
    /// สร้าง Production Orders สำหรับกลุ่มของ transactions
    /// (1 BOM = 1 order ต่อกลุ่ม)
    /// </summary>
    private async Task<IReadOnlyList<ProductionOrderDto>> CreateProductionOrdersForGroupAsync(
        IReadOnlyList<ErpSalesTransactionDto> groupTransactions,
        string[] sourceDocNos,
        DateOnly sourceDocDateFrom,
        DateOnly sourceDocDateTo,
        IReadOnlyDictionary<string, Guid> assignmentMap,
        CalculateSalesProductionRequest request,
        CancellationToken ct)
    {
        var createdOrders = new List<ProductionOrderDto>();

        // Group by item_code เพื่อสร้าง 1 order ต่อสินค้าต่อกลุ่ม
        var byItem = groupTransactions.GroupBy(t => t.ItemCode);

        foreach (var itemGroup in byItem)
        {
            var itemCode = itemGroup.Key;
            if (!assignmentMap.TryGetValue(itemCode, out var bomId))
                continue;

            var bom = await bomRepository.GetByIdAsync(bomId, ct);
            if (bom is null || bom.Status != "Active")
                continue;

            decimal totalQtyBase = itemGroup.Sum(t => t.QtyInBaseUnit);

            // คำนวณ materials สำหรับ order นี้
            var materials = new Dictionary<string, (string Name, decimal Qty, string Unit)>();
            await ExpandBomAsync(bom, totalQtyBase, materials, new HashSet<Guid>(), depth: 0, ct);

            // Serialize BOM snapshot
            var bomSnapshot = JsonSerializer.Serialize(bom);

            var cmd = new CreateProductionOrderInternalCommand(
                BomId: bomId,
                BomCode: bom.Code,
                ItemCode: itemCode,
                ItemName: bom.ItemName,
                Quantity: totalQtyBase,
                BomSnapshot: bomSnapshot,
                SourceSoNumbers: sourceDocNos,
                SourceDocDateFrom: sourceDocDateFrom,
                SourceDocDateTo: sourceDocDateTo,
                CreatedBy: request.CreatedBy,
                CreatedVia: request.CreatedVia,
                Notes: null);

            var order = await productionOrderRepository.CreateAsync(cmd, ct);
            createdOrders.Add(order);
        }

        return createdOrders;
    }

    /// <summary>
    /// ขยาย BOM แบบ recursive (multi-level BOM expansion) ด้วย DFS
    /// จำกัด depth สูงสุดที่ MaxBomDepth (10 ระดับ) เพื่อป้องกัน infinite loop
    /// สูตร: (totalQtyInBaseUnit / bom.YieldQuantity) × bomLine.Quantity
    /// </summary>
    private async Task ExpandBomAsync(
        BomDto bom,
        decimal totalQtyInBaseUnit,
        Dictionary<string, (string Name, decimal Qty, string Unit)> materials,
        HashSet<Guid> visited,
        int depth,
        CancellationToken ct)
    {
        if (depth >= MaxBomDepth)
            return;

        if (!visited.Add(bom.Id))
            return; // ป้องกัน circular reference

        // สัดส่วนการผลิต = ยอดที่ต้องการ / จำนวนที่ผลิตได้ต่อรอบ
        decimal ratio = bom.YieldQuantity == 0 ? totalQtyInBaseUnit : totalQtyInBaseUnit / bom.YieldQuantity;

        foreach (var line in bom.Lines)
        {
            decimal requiredQty = ratio * line.Quantity;

            if (line.SubBomId.HasValue)
            {
                // Sub-assembly: ขยาย BOM ต่อแบบ recursive
                var subBom = await bomRepository.GetByIdAsync(line.SubBomId.Value, ct);
                if (subBom is not null && subBom.Status == "Active")
                {
                    await ExpandBomAsync(subBom, requiredQty, materials, new HashSet<Guid>(visited), depth + 1, ct);
                    continue;
                }
            }

            // Raw material: บันทึก material requirement
            if (materials.TryGetValue(line.MaterialCode, out var existing))
                materials[line.MaterialCode] = (line.MaterialName, existing.Qty + requiredQty, line.Unit);
            else
                materials[line.MaterialCode] = (line.MaterialName, requiredQty, line.Unit);
        }

        visited.Remove(bom.Id);
    }
}
