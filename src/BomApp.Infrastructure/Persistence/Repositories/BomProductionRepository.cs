using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Entities;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BomApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation for bom_productions, bom_production_orders, and bom_production_details.
/// </summary>
public class BomProductionRepository(BomDbContext context) : IBomProductionRepository
{
    public async Task<IReadOnlyList<BomProductionDto>> GetAllAsync(
        DateOnly? docDateFrom = null,
        DateOnly? docDateTo = null,
        string? docNo = null,
        string? itemCode = null,
        CancellationToken ct = default)
    {
        var query = context.BomProductions
            .AsNoTracking()
            .Include(p => p.Orders)
            .Include(p => p.Details)
            .AsQueryable();

        if (docDateFrom.HasValue)
            query = query.Where(p => p.DocDate >= docDateFrom.Value);

        if (docDateTo.HasValue)
            query = query.Where(p => p.DocDate <= docDateTo.Value);

        if (!string.IsNullOrWhiteSpace(docNo))
            query = query.Where(p => p.DocNo.Contains(docNo));

        if (!string.IsNullOrWhiteSpace(itemCode))
            query = query.Where(p =>
                p.Orders.Any(o => o.ItemCode.Contains(itemCode)) ||
                p.Details.Any(d => d.ItemCode.Contains(itemCode)));

        var productions = await query
            .OrderByDescending(p => p.DocDate)
            .ThenByDescending(p => p.DocNo)
            .ToListAsync(ct);

        return productions.Select(MapToDto).ToList();
    }

    public async Task<BomProductionDto?> GetByDocNoAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var production = await context.BomProductions
            .AsNoTracking()
            .Include(p => p.Orders)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.DocNo == docNo, ct);

        return production is null ? null : MapToDto(production);
    }

    public async Task<IReadOnlyList<BomProductionOrderDto>> GetOrdersByDocNoAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var orders = await context.BomProductionOrders
            .AsNoTracking()
            .Where(o => o.DocNo == docNo)
            .OrderBy(o => o.RefDocNo)
            .ThenBy(o => o.ItemCode)
            .ToListAsync(ct);

        return orders.Select(MapOrderToDto).ToList();
    }

    public async Task<IReadOnlyList<BomProductionDetailDto>> GetDetailsByDocNoAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var details = await context.BomProductionDetails
            .AsNoTracking()
            .Where(d => d.DocNo == docNo)
            .OrderBy(d => d.ItemCode)
            .ToListAsync(ct);

        return details.Select(MapDetailToDto).ToList();
    }

    public async Task<bool> DeleteByDocNoAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var production = await context.BomProductions
            .FirstOrDefaultAsync(p => p.DocNo == docNo, ct);

        if (production is null)
            return false;

        context.BomProductions.Remove(production);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<BomProductionDto> CreateAsync(
        CreateBomProductionInternalCommand cmd,
        CancellationToken ct = default)
    {
        var docNo = await GenerateDocNoAsync(cmd.DocDate, ct);

        var production = new BomProduction
        {
            Id = Guid.NewGuid(),
            DocNo = docNo,
            DocDate = cmd.DocDate,
            DocTime = cmd.DocTime,
            Orders = cmd.Orders.Select(o => new BomProductionOrder
            {
                Id = Guid.NewGuid(),
                DocNo = docNo,
                DocDate = cmd.DocDate,
                RefDocNo = o.RefDocNo,
                RefDocDate = o.RefDocDate,
                ItemCode = o.ItemCode,
                Qty = o.Qty,
                UnitCode = o.UnitCode
            }).ToList(),
            Details = cmd.Details.Select(d => new BomProductionDetail
            {
                Id = Guid.NewGuid(),
                DocNo = docNo,
                ItemCode = d.ItemCode,
                ItemName = d.ItemName,
                Qty = d.Qty,
                UnitCode = d.UnitCode
            }).ToList()
        };

        context.BomProductions.Add(production);
        await context.SaveChangesAsync(ct);

        return MapToDto(production);
    }

    private async Task<string> GenerateDocNoAsync(DateOnly docDate, CancellationToken ct)
    {
        var prefix = $"BP-{docDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-";

        var lastDocNo = await context.BomProductions
            .AsNoTracking()
            .Where(p => p.DocNo.StartsWith(prefix))
            .OrderByDescending(p => p.DocNo)
            .Select(p => p.DocNo)
            .FirstOrDefaultAsync(ct);

        var nextSeq = 1;
        if (lastDocNo is not null)
        {
            var lastSeqText = lastDocNo[prefix.Length..];
            if (int.TryParse(lastSeqText, out var lastSeq))
                nextSeq = lastSeq + 1;
        }

        return $"{prefix}{nextSeq:D5}";
    }

    private static BomProductionDto MapToDto(BomProduction production) => new(
        Id: production.Id,
        DocDate: production.DocDate,
        DocNo: production.DocNo,
        DocTime: production.DocTime,
        Orders: production.Orders.Select(MapOrderToDto).ToList(),
        Details: production.Details.Select(MapDetailToDto).ToList());

    private static BomProductionOrderDto MapOrderToDto(BomProductionOrder order) => new(
        Id: order.Id,
        DocNo: order.DocNo,
        DocDate: order.DocDate,
        RefDocNo: order.RefDocNo,
        RefDocDate: order.RefDocDate,
        ItemCode: order.ItemCode,
        Qty: order.Qty,
        UnitCode: order.UnitCode);

    private static BomProductionDetailDto MapDetailToDto(BomProductionDetail detail) => new(
        Id: detail.Id,
        DocNo: detail.DocNo,
        ItemCode: detail.ItemCode,
        ItemName: detail.ItemName,
        Qty: detail.Qty,
        UnitCode: detail.UnitCode);
}
