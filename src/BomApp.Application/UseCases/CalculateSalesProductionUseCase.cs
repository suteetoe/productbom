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
    IBomProductionRepository bomProductionRepository,
    IErpProductionRepository erpProductionRepository,
    IErpStockRequestProcessor erpStockRequestProcessor)
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
    /// บันทึกรายการขายที่เลือกไว้ใน bom_production_orders จากผลการคำนวณ
    /// เรียก CalculateAsync ภายในก่อน จากนั้นสร้างเลขเอกสารตาม SaveMode
    /// </summary>
    public async Task<Result<IReadOnlyList<BomProductionDto>>> SaveAsync(
        CalculateSalesProductionRequest request,
        CancellationToken ct = default)
    {
        // คำนวณก่อนเสมอ
        try
        {
            var calcResult = await CalculateAsync(request, ct);
            if (!calcResult.IsSuccess)
                return Result<IReadOnlyList<BomProductionDto>>.Failure(calcResult.Error!);

            // ดึงรายการขายอีกครั้งเพื่อสร้างเอกสาร (ต้องการ doc grouping)
            var transactions = await erpSalesOrderRepository
                .GetSalesTransactionsByDateRangeAsync(request.DateFrom, request.DateTo, ct);

            var allItemCodes = transactions.Select(t => t.ItemCode).Distinct().ToList();
            var assignmentMap = await bomAssignmentRepository.GetAssignedItemCodesAsync(allItemCodes, ct);

            var eligibleTransactions = transactions
                .Where(t => assignmentMap.ContainsKey(t.ItemCode))
                .ToList();

            var createdDocuments = new List<BomProductionDto>();

            if (request.Mode == SaveMode.Daily)
            {
                // รวม 1 เอกสารต่อวัน
                var byDate = eligibleTransactions.GroupBy(t => t.DocDate);
                foreach (var dateGroup in byDate)
                {
                    var document = await CreateBomProductionForGroupAsync(
                        dateGroup.ToList(),
                        docDate: dateGroup.Key,
                        assignmentMap,
                        ct);

                    if (document is not null)
                        createdDocuments.Add(document);
                }
            }
            else // SaveMode.PerDocument
            {
                // แยกตามเลขเอกสารขาย
                var byDocNo = eligibleTransactions.GroupBy(t => t.DocNo);
                foreach (var docGroup in byDocNo)
                {
                    var document = await CreateBomProductionForGroupAsync(
                        docGroup.ToList(),
                        docDate: docGroup.First().DocDate,
                        assignmentMap,
                        ct);

                    if (document is not null)
                        createdDocuments.Add(document);
                }
            }

            return Result<IReadOnlyList<BomProductionDto>>.Success(createdDocuments);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<BomProductionDto>>.Failure(
                $"บันทึกเอกสารหรือสั่ง ERP ประมวลผลไม่สำเร็จ: {ex.Message}");
        }
    }

    /// <summary>
    /// สร้างรายการ bom_production_orders สำหรับกลุ่มของ transactions
    /// 1 กลุ่ม = 1 doc_no, แต่ละรายการขายที่เลือก = 1 row
    /// </summary>
    private async Task<BomProductionDto?> CreateBomProductionForGroupAsync(
        IReadOnlyList<ErpSalesTransactionDto> groupTransactions,
        DateOnly docDate,
        IReadOnlyDictionary<string, Guid> assignmentMap,
        CancellationToken ct)
    {
        var orders = new List<CreateBomProductionOrderInternalCommand>();
        var materialTotals = new Dictionary<(string Code, string Unit, string WhCode, string ShelfCode), (string Name, decimal Qty)>();

        // Store the selected sales items with a back-reference to the source sales bill.
        var bySalesItem = groupTransactions.GroupBy(t => new
        {
            t.DocNo,
            t.DocDate,
            t.ItemCode,
            t.UnitCode,
            t.WhCode,
            t.ShelfCode
        });

        foreach (var itemGroup in bySalesItem)
        {
            var itemCode = itemGroup.Key.ItemCode;
            if (!assignmentMap.TryGetValue(itemCode, out var bomId))
                continue;

            var bom = await bomRepository.GetByIdAsync(bomId, ct);
            if (bom is null || bom.Status != "Active")
                continue;

            decimal saleQty = itemGroup.Sum(t => t.Qty);
            if (saleQty <= 0)
                continue;

            orders.Add(new CreateBomProductionOrderInternalCommand(
                RefDocNo: itemGroup.Key.DocNo,
                RefDocDate: itemGroup.Key.DocDate,
                ItemCode: itemCode,
                Qty: saleQty,
                UnitCode: itemGroup.Key.UnitCode));

            var qtyInBaseUnit = itemGroup.Sum(t => t.QtyInBaseUnit);
            var itemMaterials = new Dictionary<string, (string Name, decimal Qty, string Unit)>();
            await ExpandBomAsync(bom, qtyInBaseUnit, itemMaterials, new HashSet<Guid>(), depth: 0, ct);

            foreach (var (materialCode, (materialName, requiredQty, unit)) in itemMaterials)
            {
                var key = (materialCode, unit, itemGroup.Key.WhCode, itemGroup.Key.ShelfCode);
                if (materialTotals.TryGetValue(key, out var existing))
                    materialTotals[key] = (materialName, existing.Qty + requiredQty);
                else
                    materialTotals[key] = (materialName, requiredQty);
            }
        }

        if (orders.Count == 0)
            return null;

        var details = materialTotals
            .Select(kv => new CreateBomProductionDetailInternalCommand(
                ItemCode: kv.Key.Code,
                ItemName: kv.Value.Name,
                Qty: kv.Value.Qty,
                UnitCode: kv.Key.Unit,
                WhCode: kv.Key.WhCode,
                ShelfCode: kv.Key.ShelfCode))
            .ToList();

        var cmd = new CreateBomProductionInternalCommand(
            DocDate: docDate,
            DocTime: TimeOnly.FromDateTime(DateTime.Now),
            Orders: orders,
            Details: details);

        var document = await bomProductionRepository.CreateAsync(cmd, ct);
        await erpProductionRepository.SaveProductionDocumentAsync(document, ct);
        await erpStockRequestProcessor.ProcessStockRequestAsync(
            document.Details
                .Select(d => d.ItemCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ct);

        return document;
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
