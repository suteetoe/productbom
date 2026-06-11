using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Erp;

/// <summary>
/// EF Core implementation ของ IErpItemRepository
/// อ่านข้อมูลจาก erp-database (ic_inventory, ic_unit_use, ic_unit) — read-only
/// </summary>
public class ErpItemRepository : IErpItemRepository
{
    private readonly ErpDbContext? _context;
    private readonly IDbContextFactory<ErpDbContext>? _contextFactory;

    public ErpItemRepository(ErpDbContext context)
    {
        _context = context;
    }

    public ErpItemRepository(IDbContextFactory<ErpDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>ดึงสินค้าทั้งหมดจาก ic_inventory</summary>
    public async Task<IReadOnlyList<ErpItemDto>> GetAllItemsAsync(CancellationToken ct = default)
    {
        await using var lease = await CreateContextLeaseAsync(ct);
        var context = lease.Context;
        var rows = await context.IcInventories
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .Select(i => new { i.Code, i.Name1, i.UnitCost })
            .ToListAsync(ct);

        return rows.Select(r => new ErpItemDto(r.Code, r.Name1, r.UnitCost)).ToList();
    }

    /// <summary>ดึงสินค้าจาก ic_inventory แบบแบ่งหน้า พร้อมค้นหา code/name_1</summary>
    public async Task<PagedResult<ErpItemDto>> GetItemsPageAsync(
        ErpItemListQuery query,
        CancellationToken ct = default)
    {
        await using var lease = await CreateContextLeaseAsync(ct);
        var context = lease.Context;
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Max(1, query.PageSize);

        var itemsQuery = context.IcInventories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchText = query.SearchText.Trim().ToLower();
            itemsQuery = itemsQuery.Where(i =>
                i.Code.ToLower().Contains(searchText) ||
                i.Name1.ToLower().Contains(searchText));
        }

        var totalCount = await itemsQuery.CountAsync(ct);
        var rows = await itemsQuery
            .OrderBy(i => i.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new { i.Code, i.Name1, i.UnitCost })
            .ToListAsync(ct);

        return new PagedResult<ErpItemDto>(
            rows.Select(r => new ErpItemDto(r.Code, r.Name1, r.UnitCost)).ToList(),
            totalCount,
            pageNumber,
            pageSize);
    }

    /// <summary>ค้นหาสินค้าตาม code หรือ name_1 (case-insensitive)</summary>
    public async Task<IReadOnlyList<ErpItemDto>> SearchItemsAsync(
        string keyword,
        CancellationToken ct = default)
    {
        await using var lease = await CreateContextLeaseAsync(ct);
        var context = lease.Context;
        var lower = keyword.ToLower();
        var rows = await context.IcInventories
            .AsNoTracking()
            .Where(i =>
                i.Code.ToLower().Contains(lower) ||
                i.Name1.ToLower().Contains(lower))
            .OrderBy(i => i.Code)
            .Select(i => new { i.Code, i.Name1, i.UnitCost })
            .ToListAsync(ct);

        return rows.Select(r => new ErpItemDto(r.Code, r.Name1, r.UnitCost)).ToList();
    }

    /// <summary>ดึงสินค้าตาม code — คืน null ถ้าไม่พบ</summary>
    public async Task<ErpItemDto?> GetItemByCodeAsync(string code, CancellationToken ct = default)
    {
        await using var lease = await CreateContextLeaseAsync(ct);
        var context = lease.Context;
        var row = await context.IcInventories
            .AsNoTracking()
            .Where(i => i.Code == code)
            .Select(i => new { i.Code, i.Name1, i.UnitCost })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : new ErpItemDto(row.Code, row.Name1, row.UnitCost);
    }

    /// <summary>ดึงหน่วยนับทั้งหมดของสินค้าจาก ic_unit_use (ใช้ raw SQL เพื่อ JOIN ic_unit)</summary>
    public async Task<IReadOnlyList<ErpUnitDto>> GetUnitsByItemCodeAsync(
        string icCode,
        CancellationToken ct = default)
    {
        await using var lease = await CreateContextLeaseAsync(ct);
        var context = lease.Context;
        var units = await context.Database
            .SqlQuery<ErpUnitRaw>($"""
                SELECT
                    u.code       AS Code,
                    u.name_1     AS Name,
                    uu.ic_code   AS IcCode,
                    uu.stand_value  AS StandValue,
                    uu.divide_value AS DivideValue,
                    uu.ratio        AS Ratio,
                    uu.line_number  AS LineNumber
                FROM ic_unit_use uu
                JOIN ic_unit u ON u.code = uu.code
                WHERE uu.ic_code = {icCode}
                ORDER BY uu.line_number
                """)
            .AsNoTracking()
            .ToListAsync(ct);

        return units.Select(r => new ErpUnitDto(
            Code: r.Code,
            Name: r.Name,
            IcCode: r.IcCode,
            StandValue: r.StandValue,
            DivideValue: r.DivideValue,
            Ratio: r.Ratio,
            LineNumber: r.LineNumber)).ToList();
    }

    /// <summary>ดึงหน่วยนับทั้งหมดจาก ic_unit (master)</summary>
    public async Task<IReadOnlyList<ErpUnitDto>> GetAllUnitsAsync(CancellationToken ct = default)
    {
        await using var lease = await CreateContextLeaseAsync(ct);
        var context = lease.Context;
        var units = await context.Database
            .SqlQuery<ErpUnitRaw>($"""
                SELECT
                    u.code       AS Code,
                    u.name_1     AS Name,
                    uu.ic_code   AS IcCode,
                    uu.stand_value  AS StandValue,
                    uu.divide_value AS DivideValue,
                    uu.ratio        AS Ratio,
                    uu.line_number  AS LineNumber
                FROM ic_unit_use uu
                JOIN ic_unit u ON u.code = uu.code
                ORDER BY u.code, uu.line_number
                """)
            .AsNoTracking()
            .ToListAsync(ct);

        return units.Select(r => new ErpUnitDto(
            Code: r.Code,
            Name: r.Name,
            IcCode: r.IcCode,
            StandValue: r.StandValue,
            DivideValue: r.DivideValue,
            Ratio: r.Ratio,
            LineNumber: r.LineNumber)).ToList();
    }

    private async ValueTask<ContextLease> CreateContextLeaseAsync(CancellationToken ct)
    {
        if (_contextFactory is null)
            return new ContextLease(_context!, ownsContext: false);

        var context = await _contextFactory.CreateDbContextAsync(ct);
        return new ContextLease(context, ownsContext: true);
    }

    private sealed class ContextLease(ErpDbContext context, bool ownsContext) : IAsyncDisposable
    {
        public ErpDbContext Context { get; } = context;

        public ValueTask DisposeAsync()
        {
            return ownsContext ? Context.DisposeAsync() : ValueTask.CompletedTask;
        }
    }

    // Internal projection type สำหรับ raw SQL query
    private sealed class ErpUnitRaw
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IcCode { get; set; } = string.Empty;
        public decimal StandValue { get; set; }
        public decimal DivideValue { get; set; }
        public int Ratio { get; set; }
        public int LineNumber { get; set; }
    }
}
