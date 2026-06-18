using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Services;

public class ProductManufacturingService(
    IProductManufacturingRepository repository,
    IBomAssignmentRepository bomAssignmentRepository,
    IBomRepository bomRepository,
    IErpItemRepository erpItemRepository,
    IErpProductionRepository erpProductionRepository) : IProductManufacturingService
{
    private const int MaxBomDepth = 10;

    public async Task<Result<PagedResult<ProductManufacturingDto>>> GetDocumentsPageAsync(
        ProductManufacturingListQuery query,
        CancellationToken ct = default)
    {
        if (query.PageNumber < 1)
            return Result<PagedResult<ProductManufacturingDto>>.Failure("Page number must be at least 1.");

        if (query.PageSize < 1)
            return Result<PagedResult<ProductManufacturingDto>>.Failure("Page size must be at least 1.");

        var page = await repository.GetPageAsync(query, ct);
        var hydratedItems = new List<ProductManufacturingDto>(page.Items.Count);
        foreach (var item in page.Items)
            hydratedItems.Add(await HydrateItemNamesAsync(item, ct));

        return Result<PagedResult<ProductManufacturingDto>>.Success(
            new PagedResult<ProductManufacturingDto>(hydratedItems, page.TotalCount, page.PageNumber, page.PageSize));
    }

    public async Task<Result<ProductManufacturingDto>> GetDocumentByDocNoAsync(
        string docNo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(docNo))
            return Result<ProductManufacturingDto>.Failure("Document number is required.");

        var document = await repository.GetByDocNoAsync(docNo.Trim(), ct);
        if (document is null)
            return Result<ProductManufacturingDto>.Failure($"Product manufacturing document not found: {docNo}");

        return Result<ProductManufacturingDto>.Success(await HydrateItemNamesAsync(document, ct));
    }

    public async Task<Result<ProductManufacturingDto>> CalculateAsync(
        CalculateProductManufacturingRequest request,
        CancellationToken ct = default)
    {
        var validation = ValidateHeader(request.DocNo, request.WhCode, request.ShelfCode);
        if (validation is not null)
            return Result<ProductManufacturingDto>.Failure(validation);

        var finishGoodValidation = ValidateFinishGoods(request.FinishGoods);
        if (finishGoodValidation is not null)
            return Result<ProductManufacturingDto>.Failure(finishGoodValidation);

        var materialResult = await CalculateMaterialsAsync(request.FinishGoods, request.WhCode, request.ShelfCode, ct);
        if (!materialResult.IsSuccess)
            return Result<ProductManufacturingDto>.Failure(materialResult.Error!);

        var finishGoods = await HydrateFinishGoodsAsync(request.DocNo, request.FinishGoods, ct);
        var document = new ProductManufacturingDto(
            request.DocNo.Trim(),
            request.DocDate,
            request.WhCode.Trim(),
            request.ShelfCode.Trim(),
            request.Remark.Trim(),
            finishGoods,
            materialResult.Value!);

        return Result<ProductManufacturingDto>.Success(document);
    }

    public async Task<Result<ProductManufacturingDto>> CreateAsync(
        CreateProductManufacturingCommand command,
        CancellationToken ct = default)
    {
        var validation = Validate(command.DocNo, command.WhCode, command.ShelfCode, command.FinishGoods, command.Materials);
        if (validation is not null)
            return Result<ProductManufacturingDto>.Failure(validation);

        var existing = await repository.GetByDocNoAsync(command.DocNo.Trim(), ct);
        if (existing is not null)
            return Result<ProductManufacturingDto>.Failure($"Document number already exists: {command.DocNo}");

        try
        {
            var document = await repository.CreateAsync(command, ct);
            var hydrated = await HydrateItemNamesAsync(document, ct);
            await erpProductionRepository.SaveProductManufacturingDocumentAsync(hydrated, ct);
            return Result<ProductManufacturingDto>.Success(hydrated);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<ProductManufacturingDto>.Failure($"Failed to save product manufacturing to ERP: {ex.Message}");
        }
    }

    public async Task<Result<ProductManufacturingDto>> UpdateAsync(
        string docNo,
        UpdateProductManufacturingCommand command,
        CancellationToken ct = default)
    {
        var validation = Validate(docNo, command.WhCode, command.ShelfCode, command.FinishGoods, command.Materials);
        if (validation is not null)
            return Result<ProductManufacturingDto>.Failure(validation);

        try
        {
            var document = await repository.UpdateAsync(docNo.Trim(), command, ct);
            if (document is null)
                return Result<ProductManufacturingDto>.Failure($"Product manufacturing document not found: {docNo}");

            var hydrated = await HydrateItemNamesAsync(document, ct);
            await erpProductionRepository.SaveProductManufacturingDocumentAsync(hydrated, ct);
            return Result<ProductManufacturingDto>.Success(hydrated);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<ProductManufacturingDto>.Failure($"Failed to save product manufacturing to ERP: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(
        string docNo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(docNo))
            return Result.Failure("Document number is required.");

        var trimmedDocNo = docNo.Trim();
        var deleted = await repository.DeleteAsync(trimmedDocNo, ct);
        if (!deleted)
            return Result.Failure($"Product manufacturing document not found: {docNo}");

        try
        {
            await erpProductionRepository.DeleteProductManufacturingDocumentAsync(trimmedDocNo, ct);
            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete product manufacturing from ERP: {ex.Message}");
        }
    }

    private async Task<Result<IReadOnlyList<ProductManufacturingMaterialDto>>> CalculateMaterialsAsync(
        IReadOnlyList<CreateProductManufacturingFinishGoodCommand> finishGoods,
        string whCode,
        string shelfCode,
        CancellationToken ct)
    {
        var itemCodes = finishGoods.Select(f => f.ItemCode.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var assignmentMap = await bomAssignmentRepository.GetAssignedItemCodesAsync(itemCodes, ct);
        var missingBomCodes = itemCodes.Where(code => !assignmentMap.ContainsKey(code)).ToList();
        if (missingBomCodes.Count > 0)
            return Result<IReadOnlyList<ProductManufacturingMaterialDto>>.Failure(
                $"No active BOM assignment for item(s): {string.Join(", ", missingBomCodes)}");

        var materialTotals = new Dictionary<(string Code, string Unit, string WhCode, string ShelfCode), (string Name, decimal Qty)>();
        var materialNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var finishGood in finishGoods)
        {
            var itemCode = finishGood.ItemCode.Trim();
            var bom = await bomRepository.GetByIdAsync(assignmentMap[itemCode], ct);
            if (bom is null || bom.Status != "Active")
                return Result<IReadOnlyList<ProductManufacturingMaterialDto>>.Failure($"BOM for item {itemCode} is not active.");

            var qtyInYieldUnit = await ConvertQuantityAsync(
                finishGood.Qty,
                finishGood.UnitCode,
                itemCode,
                bom.YieldUnit,
                ct);

            var itemMaterials = new Dictionary<string, (string Name, decimal Qty, string Unit)>();
            await ExpandBomAsync(bom, qtyInYieldUnit, itemMaterials, materialNameCache, new HashSet<Guid>(), 0, ct);

            foreach (var (materialCode, (materialName, requiredQty, unit)) in itemMaterials)
            {
                var key = (materialCode, unit, whCode.Trim(), shelfCode.Trim());
                if (materialTotals.TryGetValue(key, out var existing))
                    materialTotals[key] = (PreferName(existing.Name, materialName), existing.Qty + requiredQty);
                else
                    materialTotals[key] = (materialName, requiredQty);
            }
        }

        var lineNumber = 1;
        var materials = materialTotals
            .OrderBy(kv => kv.Key.Code)
            .Select(kv => new ProductManufacturingMaterialDto(
                string.Empty,
                kv.Key.Code,
                kv.Value.Name,
                kv.Value.Qty,
                kv.Key.Unit,
                kv.Key.WhCode,
                kv.Key.ShelfCode,
                lineNumber++))
            .ToList();

        return Result<IReadOnlyList<ProductManufacturingMaterialDto>>.Success(materials);
    }

    private async Task ExpandBomAsync(
        BomDto bom,
        decimal totalQtyInYieldUnit,
        Dictionary<string, (string Name, decimal Qty, string Unit)> materials,
        Dictionary<string, string> materialNameCache,
        HashSet<Guid> visited,
        int depth,
        CancellationToken ct)
    {
        if (depth >= MaxBomDepth)
            return;

        if (!visited.Add(bom.Id))
            return;

        var ratio = bom.YieldQuantity == 0 ? totalQtyInYieldUnit : totalQtyInYieldUnit / bom.YieldQuantity;
        foreach (var line in bom.Lines)
        {
            var requiredQty = ratio * line.Quantity;
            if (line.SubBomId.HasValue)
            {
                var subBom = await bomRepository.GetByIdAsync(line.SubBomId.Value, ct);
                if (subBom is not null && subBom.Status == "Active")
                {
                    var subBomQty = await ConvertQuantityAsync(requiredQty, line.Unit, subBom.ItemCode, subBom.YieldUnit, ct);
                    await ExpandBomAsync(subBom, subBomQty, materials, materialNameCache, new HashSet<Guid>(visited), depth + 1, ct);
                    continue;
                }
            }

            var materialName = await ResolveItemNameAsync(line.MaterialCode, line.MaterialName, materialNameCache, ct);
            if (materials.TryGetValue(line.MaterialCode, out var existing))
                materials[line.MaterialCode] = (PreferName(existing.Name, materialName), existing.Qty + requiredQty, line.Unit);
            else
                materials[line.MaterialCode] = (materialName, requiredQty, line.Unit);
        }

        visited.Remove(bom.Id);
    }

    private async Task<decimal> ConvertQuantityAsync(
        decimal quantity,
        string fromUnit,
        string itemCode,
        string toUnit,
        CancellationToken ct)
    {
        if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
            return quantity;

        var units = await erpItemRepository.GetUnitsByItemCodeAsync(itemCode, ct);
        var source = units.FirstOrDefault(u => string.Equals(u.Code, fromUnit, StringComparison.OrdinalIgnoreCase));
        var target = units.FirstOrDefault(u => string.Equals(u.Code, toUnit, StringComparison.OrdinalIgnoreCase));

        if (source is null || target is null || target.StandValue == 0)
            return quantity;

        var quantityInBaseUnit = source.ToBaseUnit(quantity);
        return quantityInBaseUnit * target.DivideValue / target.StandValue;
    }

    private async Task<IReadOnlyList<ProductManufacturingFinishGoodDto>> HydrateFinishGoodsAsync(
        string docNo,
        IReadOnlyList<CreateProductManufacturingFinishGoodCommand> finishGoods,
        CancellationToken ct)
    {
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<ProductManufacturingFinishGoodDto>(finishGoods.Count);

        foreach (var finishGood in finishGoods.OrderBy(f => f.LineNumber))
        {
            var itemName = await ResolveItemNameAsync(finishGood.ItemCode, string.Empty, cache, ct);
            rows.Add(new ProductManufacturingFinishGoodDto(
                docNo.Trim(),
                finishGood.ItemCode.Trim(),
                itemName,
                finishGood.Qty,
                finishGood.UnitCode.Trim(),
                finishGood.WhCode.Trim(),
                finishGood.ShelfCode.Trim(),
                finishGood.LineNumber));
        }

        return rows;
    }

    private async Task<ProductManufacturingDto> HydrateItemNamesAsync(
        ProductManufacturingDto document,
        CancellationToken ct)
    {
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var finishGoods = new List<ProductManufacturingFinishGoodDto>(document.FinishGoods.Count);
        var materials = new List<ProductManufacturingMaterialDto>(document.Materials.Count);

        foreach (var finishGood in document.FinishGoods)
            finishGoods.Add(finishGood with
            {
                ItemName = await ResolveItemNameAsync(finishGood.ItemCode, finishGood.ItemName, cache, ct)
            });

        foreach (var material in document.Materials)
            materials.Add(material with
            {
                ItemName = await ResolveItemNameAsync(material.ItemCode, material.ItemName, cache, ct)
            });

        return document with { FinishGoods = finishGoods, Materials = materials };
    }

    private async Task<string> ResolveItemNameAsync(
        string itemCode,
        string currentName,
        Dictionary<string, string> cache,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(currentName))
            return currentName;

        if (cache.TryGetValue(itemCode, out var cachedName))
            return cachedName;

        var item = await erpItemRepository.GetItemByCodeAsync(itemCode, ct);
        var name = string.IsNullOrWhiteSpace(item?.Name) ? itemCode : item.Name;
        cache[itemCode] = name;
        return name;
    }

    private static string? Validate(
        string docNo,
        string whCode,
        string shelfCode,
        IReadOnlyList<CreateProductManufacturingFinishGoodCommand> finishGoods,
        IReadOnlyList<CreateProductManufacturingMaterialCommand> materials)
    {
        var headerValidation = ValidateHeader(docNo, whCode, shelfCode);
        if (headerValidation is not null)
            return headerValidation;

        var finishGoodValidation = ValidateFinishGoods(finishGoods);
        if (finishGoodValidation is not null)
            return finishGoodValidation;

        if (materials.Count == 0)
            return "At least one material line is required.";

        if (materials.Any(d => string.IsNullOrWhiteSpace(d.ItemCode)))
            return "Every material line must have an item code.";

        if (materials.Any(d => d.Qty <= 0))
            return "Every material line quantity must be greater than zero.";

        return null;
    }

    private static string? ValidateHeader(string docNo, string whCode, string shelfCode)
    {
        if (string.IsNullOrWhiteSpace(docNo))
            return "Document number is required.";

        if (string.IsNullOrWhiteSpace(whCode))
            return "Warehouse is required.";

        if (string.IsNullOrWhiteSpace(shelfCode))
            return "Location is required.";

        return null;
    }

    private static string? ValidateFinishGoods(IReadOnlyList<CreateProductManufacturingFinishGoodCommand> finishGoods)
    {
        if (finishGoods.Count == 0)
            return "At least one finished good line is required.";

        if (finishGoods.Any(d => string.IsNullOrWhiteSpace(d.ItemCode)))
            return "Every finished good line must have an item code.";

        if (finishGoods.Any(d => d.Qty <= 0))
            return "Every finished good line quantity must be greater than zero.";

        if (finishGoods.Any(d => string.IsNullOrWhiteSpace(d.UnitCode)))
            return "Every finished good line must have a unit.";

        return null;
    }

    private static string PreferName(string currentName, string candidateName)
        => string.IsNullOrWhiteSpace(currentName) ? candidateName : currentName;
}
