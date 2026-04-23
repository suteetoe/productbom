using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Entities;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation ของ IProductionOrderRepository — ใช้ BomDbContext
/// </summary>
public class ProductionOrderRepository(BomDbContext context) : IProductionOrderRepository
{
    /// <summary>ดึง Production Orders ตาม filter</summary>
    public async Task<IReadOnlyList<ProductionOrderDto>> GetAllAsync(
        DateOnly?    dateFrom    = null,
        DateOnly?    dateTo      = null,
        string?      status      = null,
        string?      itemCode    = null,
        string?      createdVia  = null,
        string?      sourceDocNo = null,
        CancellationToken ct = default)
    {
        var query = context.ProductionOrders
            .AsNoTracking()
            .Include(p => p.Bom)
            .AsQueryable();

        if (dateFrom.HasValue)
        {
            var fromDt = dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.CreatedAt >= fromDt);
        }

        if (dateTo.HasValue)
        {
            var toDt = dateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(p => p.CreatedAt <= toDt);
        }

        if (status is not null)
            query = query.Where(p => p.Status == status);

        if (itemCode is not null)
            query = query.Where(p => p.ItemCode == itemCode);

        if (createdVia is not null)
            query = query.Where(p => p.CreatedVia == createdVia);

        if (sourceDocNo is not null)
            query = query.Where(p => p.SourceSoNumbers.Contains(sourceDocNo));

        var orders = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return orders.Select(MapToDto).ToList();
    }

    /// <summary>ดึง Production Order ตาม Id — คืน null ถ้าไม่พบ</summary>
    public async Task<ProductionOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await context.ProductionOrders
            .AsNoTracking()
            .Include(p => p.Bom)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return order is null ? null : MapToDto(order);
    }

    /// <summary>ดึงรายการวัตถุดิบของ Production Order</summary>
    public async Task<IReadOnlyList<ProductionOrderLineDto>> GetLinesByOrderIdAsync(
        Guid productionOrderId,
        CancellationToken ct = default)
    {
        var lines = await context.ProductionOrderLines
            .AsNoTracking()
            .Where(l => l.ProductionOrderId == productionOrderId)
            .OrderBy(l => l.MaterialCode)
            .ToListAsync(ct);

        return lines.Select(l => new ProductionOrderLineDto(
            Id: l.Id,
            MaterialCode: l.MaterialCode,
            MaterialName: l.MaterialName,
            RequiredQuantity: l.RequiredQuantity,
            Unit: l.Unit)).ToList();
    }

    /// <summary>
    /// ตรวจว่า doc_no เหล่านี้มีใน source_so_numbers ของ order ที่มีอยู่แล้วหรือไม่
    /// ใช้ PostgreSQL array containment
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAlreadyProcessedDocNosAsync(
        IReadOnlyList<string> docNos,
        CancellationToken ct = default)
    {
        var docNoArray = docNos.ToArray();

        // ดึง all source_so_numbers arrays ที่มี overlap กับ docNos
        var processedDocNos = await context.ProductionOrders
            .AsNoTracking()
            .Where(p => p.SourceSoNumbers.Any(s => docNoArray.Contains(s)))
            .SelectMany(p => p.SourceSoNumbers)
            .Where(s => docNoArray.Contains(s))
            .Distinct()
            .ToListAsync(ct);

        return processedDocNos;
    }

    /// <summary>สร้าง Production Order ใหม่พร้อม lines</summary>
    public async Task<ProductionOrderDto> CreateAsync(
        CreateProductionOrderInternalCommand cmd,
        CancellationToken ct = default)
    {
        var orderNo = await GenerateOrderNoAsync(ct);
        var now = DateTime.UtcNow;

        var order = new ProductionOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = orderNo,
            BomId = cmd.BomId,
            BomSnapshot = cmd.BomSnapshot,
            ItemCode = cmd.ItemCode,
            ItemName = cmd.ItemName,
            Quantity = cmd.Quantity,
            Status = "Pending",
            SourceSoNumbers = cmd.SourceSoNumbers,
            SourceDocDateFrom = cmd.SourceDocDateFrom,
            SourceDocDateTo = cmd.SourceDocDateTo,
            CreatedBy = cmd.CreatedBy,
            CreatedVia = cmd.CreatedVia,
            CreatedAt = now,
            Notes = cmd.Notes
        };

        context.ProductionOrders.Add(order);
        await context.SaveChangesAsync(ct);

        return MapToDto(order);
    }

    /// <summary>เปลี่ยน status ของ Production Order</summary>
    public async Task SetStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        await context.ProductionOrders
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, status), ct);
    }

    /// <summary>สร้างเลขที่ Production Order format PO-YYYYMM-NNNNN</summary>
    private async Task<string> GenerateOrderNoAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var prefix = $"PO-{now:yyyyMM}-";

        var lastOrderInMonth = await context.ProductionOrders
            .AsNoTracking()
            .Where(p => p.OrderNo.StartsWith(prefix))
            .OrderByDescending(p => p.OrderNo)
            .Select(p => p.OrderNo)
            .FirstOrDefaultAsync(ct);

        int nextSeq = 1;
        if (lastOrderInMonth is not null)
        {
            var lastSeqStr = lastOrderInMonth[(prefix.Length)..];
            if (int.TryParse(lastSeqStr, out var lastSeq))
                nextSeq = lastSeq + 1;
        }

        return $"{prefix}{nextSeq:D5}";
    }

    // ---- Mapping ----

    private static ProductionOrderDto MapToDto(ProductionOrder p) => new(
        Id: p.Id,
        OrderNo: p.OrderNo,
        BomId: p.BomId,
        BomCode: p.Bom?.Code ?? string.Empty,
        ItemCode: p.ItemCode,
        ItemName: p.ItemName,
        Quantity: p.Quantity,
        Status: p.Status,
        SourceSoNumbers: p.SourceSoNumbers,
        SourceDocDateFrom: p.SourceDocDateFrom,
        SourceDocDateTo: p.SourceDocDateTo,
        CreatedBy: p.CreatedBy,
        CreatedVia: p.CreatedVia,
        CreatedAt: p.CreatedAt,
        Notes: p.Notes);
}
