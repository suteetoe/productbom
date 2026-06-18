using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Entities;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Persistence.Repositories;

public class ProductManufacturingRepository(BomDbContext context) : IProductManufacturingRepository
{
    public async Task<PagedResult<ProductManufacturingDto>> GetPageAsync(
        ProductManufacturingListQuery query,
        CancellationToken ct = default)
    {
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Max(1, query.PageSize);

        var documentsQuery = ApplyFilters(
            context.ProductManufacturings.AsNoTracking().AsQueryable(),
            query.DocDateFrom,
            query.DocDateTo,
            query.DocNo,
            query.ItemCode);

        var totalCount = await documentsQuery.CountAsync(ct);
        var documents = await documentsQuery
            .Include(d => d.FinishGoods)
            .Include(d => d.Materials)
            .OrderByDescending(d => d.DocDate)
            .ThenByDescending(d => d.DocNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ProductManufacturingDto>(
            documents.Select(MapToDto).ToList(),
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<ProductManufacturingDto?> GetByDocNoAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var document = await context.ProductManufacturings
            .AsNoTracking()
            .Include(d => d.FinishGoods)
            .Include(d => d.Materials)
            .FirstOrDefaultAsync(d => d.DocNo == docNo, ct);

        return document is null ? null : MapToDto(document);
    }

    public async Task<ProductManufacturingDto> CreateAsync(
        CreateProductManufacturingCommand command,
        CancellationToken ct = default)
    {
        var docNo = command.DocNo.Trim();
        var document = new ProductManufacturing
        {
            DocNo = docNo,
            DocDate = command.DocDate,
            WhCode = command.WhCode.Trim(),
            ShelfCode = command.ShelfCode.Trim(),
            Remark = command.Remark.Trim(),
            TotalCost = command.FinishGoods.Sum(d => d.TotalCost),
            FinishGoods = command.FinishGoods
                .OrderBy(d => d.LineNumber)
                .Select(d => new ProductManufacturingFinishGood
                {
                    DocNo = docNo,
                    ItemCode = d.ItemCode.Trim(),
                    Qty = d.Qty,
                    UnitCode = d.UnitCode.Trim(),
                    WhCode = d.WhCode.Trim(),
                    ShelfCode = d.ShelfCode.Trim(),
                    CostPerUnit = d.CostPerUnit,
                    TotalCost = d.TotalCost,
                    LineNumber = d.LineNumber
                })
                .ToList(),
            Materials = command.Materials
                .OrderBy(d => d.LineNumber)
                .Select(d => new ProductManufacturingMaterial
                {
                    DocNo = docNo,
                    ItemCode = d.ItemCode.Trim(),
                    Qty = d.Qty,
                    UnitCode = d.UnitCode.Trim(),
                    WhCode = d.WhCode.Trim(),
                    ShelfCode = d.ShelfCode.Trim(),
                    CostPerUnit = d.CostPerUnit,
                    TotalCost = d.TotalCost,
                    LineNumber = d.LineNumber
                })
                .ToList()
        };

        context.ProductManufacturings.Add(document);
        await context.SaveChangesAsync(ct);
        return MapToDto(document);
    }

    public async Task<ProductManufacturingDto?> UpdateAsync(
        string docNo,
        UpdateProductManufacturingCommand command,
        CancellationToken ct = default)
    {
        var document = await context.ProductManufacturings
            .Include(d => d.FinishGoods)
            .Include(d => d.Materials)
            .FirstOrDefaultAsync(d => d.DocNo == docNo, ct);

        if (document is null)
            return null;

        document.DocDate = command.DocDate;
        document.WhCode = command.WhCode.Trim();
        document.ShelfCode = command.ShelfCode.Trim();
        document.Remark = command.Remark.Trim();
        document.TotalCost = command.FinishGoods.Sum(d => d.TotalCost);

        context.ProductManufacturingFinishGoods.RemoveRange(document.FinishGoods);
        context.ProductManufacturingMaterials.RemoveRange(document.Materials);

        document.FinishGoods = command.FinishGoods
            .OrderBy(d => d.LineNumber)
            .Select(d => new ProductManufacturingFinishGood
            {
                DocNo = docNo,
                ItemCode = d.ItemCode.Trim(),
                Qty = d.Qty,
                UnitCode = d.UnitCode.Trim(),
                WhCode = d.WhCode.Trim(),
                ShelfCode = d.ShelfCode.Trim(),
                CostPerUnit = d.CostPerUnit,
                TotalCost = d.TotalCost,
                LineNumber = d.LineNumber
            })
            .ToList();

        document.Materials = command.Materials
            .OrderBy(d => d.LineNumber)
            .Select(d => new ProductManufacturingMaterial
            {
                DocNo = docNo,
                ItemCode = d.ItemCode.Trim(),
                Qty = d.Qty,
                UnitCode = d.UnitCode.Trim(),
                WhCode = d.WhCode.Trim(),
                ShelfCode = d.ShelfCode.Trim(),
                CostPerUnit = d.CostPerUnit,
                TotalCost = d.TotalCost,
                LineNumber = d.LineNumber
            })
            .ToList();

        await context.SaveChangesAsync(ct);
        return MapToDto(document);
    }

    public async Task<bool> DeleteAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var document = await context.ProductManufacturings
            .Include(d => d.FinishGoods)
            .Include(d => d.Materials)
            .FirstOrDefaultAsync(d => d.DocNo == docNo, ct);

        if (document is null)
            return false;

        context.ProductManufacturings.Remove(document);
        await context.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<ProductManufacturing> ApplyFilters(
        IQueryable<ProductManufacturing> query,
        DateOnly? docDateFrom,
        DateOnly? docDateTo,
        string? docNo,
        string? itemCode)
    {
        if (docDateFrom.HasValue)
            query = query.Where(d => d.DocDate >= docDateFrom.Value);

        if (docDateTo.HasValue)
            query = query.Where(d => d.DocDate <= docDateTo.Value);

        if (!string.IsNullOrWhiteSpace(docNo))
            query = query.Where(d => d.DocNo.Contains(docNo));

        if (!string.IsNullOrWhiteSpace(itemCode))
            query = query.Where(d =>
                d.FinishGoods.Any(l => l.ItemCode.Contains(itemCode)) ||
                d.Materials.Any(l => l.ItemCode.Contains(itemCode)));

        return query;
    }

    private static ProductManufacturingDto MapToDto(ProductManufacturing document) =>
        new(
            document.DocNo,
            document.DocDate,
            document.WhCode,
            document.ShelfCode,
            document.Remark,
            document.TotalCost,
            document.FinishGoods
                .OrderBy(d => d.LineNumber)
                .Select(d => new ProductManufacturingFinishGoodDto(
                    d.DocNo,
                    d.ItemCode,
                    string.Empty,
                    d.Qty,
                    d.UnitCode,
                    d.WhCode,
                    d.ShelfCode,
                    d.CostPerUnit,
                    d.TotalCost,
                    d.LineNumber))
                .ToList(),
            document.Materials
                .OrderBy(d => d.LineNumber)
                .Select(d => new ProductManufacturingMaterialDto(
                    d.DocNo,
                    d.ItemCode,
                    string.Empty,
                    d.Qty,
                    d.UnitCode,
                    d.WhCode,
                    d.ShelfCode,
                    d.CostPerUnit,
                    d.TotalCost,
                    d.LineNumber))
                .ToList());
}
