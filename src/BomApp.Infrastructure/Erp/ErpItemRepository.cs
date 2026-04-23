using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Erp;

/// <summary>
/// EF Core implementation ของ IErpItemRepository
/// อ่านข้อมูลจาก erp-database (ic_inventory, ic_unit_use, ic_unit) — read-only
/// </summary>
public class ErpItemRepository(ErpDbContext context) : IErpItemRepository
{
    /// <summary>ดึงสินค้าทั้งหมดจาก ic_inventory</summary>
    public async Task<IReadOnlyList<ErpItemDto>> GetAllItemsAsync(CancellationToken ct = default)
    {
        var items = await context.IcInventories
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .ToListAsync(ct);

        return items.Select(i => new ErpItemDto(i.Code, i.Name1, i.UnitCost)).ToList();
    }

    /// <summary>ค้นหาสินค้าตาม code หรือ name_1 (case-insensitive)</summary>
    public async Task<IReadOnlyList<ErpItemDto>> SearchItemsAsync(
        string keyword,
        CancellationToken ct = default)
    {
        var lower = keyword.ToLower();
        var items = await context.IcInventories
            .AsNoTracking()
            .Where(i =>
                i.Code.ToLower().Contains(lower) ||
                i.Name1.ToLower().Contains(lower))
            .OrderBy(i => i.Code)
            .ToListAsync(ct);

        return items.Select(i => new ErpItemDto(i.Code, i.Name1, i.UnitCost)).ToList();
    }

    /// <summary>ดึงสินค้าตาม code — คืน null ถ้าไม่พบ</summary>
    public async Task<ErpItemDto?> GetItemByCodeAsync(string code, CancellationToken ct = default)
    {
        var item = await context.IcInventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Code == code, ct);

        return item is null ? null : new ErpItemDto(item.Code, item.Name1, item.UnitCost);
    }

    /// <summary>ดึงหน่วยนับทั้งหมดของสินค้าจาก ic_unit_use (ใช้ raw SQL เพื่อ JOIN ic_unit)</summary>
    public async Task<IReadOnlyList<ErpUnitDto>> GetUnitsByItemCodeAsync(
        string icCode,
        CancellationToken ct = default)
    {
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
