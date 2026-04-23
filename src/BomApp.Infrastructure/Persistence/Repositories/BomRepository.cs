using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Entities;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation ของ IBomRepository — ใช้ BomDbContext
/// </summary>
public class BomRepository(BomDbContext context) : IBomRepository
{
    /// <summary>ดึง BOM ทั้งหมดพร้อม lines</summary>
    public async Task<IReadOnlyList<BomDto>> GetAllAsync(CancellationToken ct = default)
    {
        var boms = await context.Boms
            .AsNoTracking()
            .Include(b => b.Lines)
            .OrderBy(b => b.Code)
            .ToListAsync(ct);

        return boms.Select(MapToDto).ToList();
    }

    /// <summary>ดึง BOM ตาม Id พร้อม lines — คืน null ถ้าไม่พบ</summary>
    public async Task<BomDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var bom = await context.Boms
            .AsNoTracking()
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return bom is null ? null : MapToDto(bom);
    }

    /// <summary>ดึง BOM ตาม Code พร้อม lines — คืน null ถ้าไม่พบ</summary>
    public async Task<BomDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var bom = await context.Boms
            .AsNoTracking()
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Code == code, ct);

        return bom is null ? null : MapToDto(bom);
    }

    /// <summary>ตรวจสอบว่า Code ซ้ำหรือไม่ (excludeId ใช้ตอน update)</summary>
    public async Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Boms.AsNoTracking().Where(b => b.Code == code);
        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    /// <summary>สร้าง BOM ใหม่พร้อม lines</summary>
    public async Task<BomDto> CreateAsync(CreateBomCommand cmd, string createdBy, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var bom = new Bom
        {
            Id = Guid.NewGuid(),
            Code = cmd.Code,
            Name = cmd.Name,
            Description = cmd.Description,
            ItemCode = cmd.ItemCode,
            ItemName = string.Empty, // ให้ caller set ถ้าต้องการ — denormalized จาก ERP
            YieldQuantity = cmd.YieldQuantity,
            YieldUnit = cmd.YieldUnit,
            Version = 1,
            Status = "Draft",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            Lines = cmd.Lines.Select((l, idx) => new BomLine
            {
                Id = Guid.NewGuid(),
                MaterialCode = l.MaterialCode,
                MaterialName = string.Empty, // denormalized — set by caller
                Quantity = l.Quantity,
                Unit = l.Unit,
                SubBomId = l.SubBomId,
                SortOrder = l.SortOrder,
                Notes = l.Notes
            }).ToList()
        };

        context.Boms.Add(bom);
        await context.SaveChangesAsync(ct);
        return MapToDto(bom);
    }

    /// <summary>แก้ไข BOM — replace lines และเพิ่ม version</summary>
    public async Task<BomDto> UpdateAsync(Guid id, UpdateBomCommand cmd, CancellationToken ct = default)
    {
        var bom = await context.Boms
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new InvalidOperationException($"BOM {id} not found");

        if (cmd.Name is not null) bom.Name = cmd.Name;
        if (cmd.Description is not null) bom.Description = cmd.Description;
        if (cmd.ItemCode is not null) bom.ItemCode = cmd.ItemCode;
        if (cmd.YieldQuantity.HasValue) bom.YieldQuantity = cmd.YieldQuantity.Value;
        if (cmd.YieldUnit is not null) bom.YieldUnit = cmd.YieldUnit;
        if (cmd.Lines is not null)
        {
            bom.Lines.Clear();
            foreach (var l in cmd.Lines)
            {
                bom.Lines.Add(new BomLine
                {
                    Id = Guid.NewGuid(),
                    BomId = bom.Id,
                    MaterialCode = l.MaterialCode,
                    MaterialName = string.Empty,
                    Quantity = l.Quantity,
                    Unit = l.Unit,
                    SubBomId = l.SubBomId,
                    SortOrder = l.SortOrder,
                    Notes = l.Notes
                });
            }
        }

        bom.Version++;
        bom.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return MapToDto(bom);
    }

    /// <summary>ลบ BOM</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var bom = await context.Boms.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"BOM {id} not found");

        context.Boms.Remove(bom);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>เปลี่ยน status ของ BOM</summary>
    public async Task SetStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        await context.Boms
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, status)
                .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), ct);
    }

    // ---- Mapping ----

    private static BomDto MapToDto(Bom b) => new(
        Id: b.Id,
        Code: b.Code,
        Name: b.Name,
        Description: b.Description,
        ItemCode: b.ItemCode,
        ItemName: b.ItemName,
        YieldQuantity: b.YieldQuantity,
        YieldUnit: b.YieldUnit,
        Version: b.Version,
        Status: b.Status,
        CreatedAt: b.CreatedAt,
        UpdatedAt: b.UpdatedAt,
        CreatedBy: b.CreatedBy,
        Lines: b.Lines.OrderBy(l => l.SortOrder).Select(MapLineToDto).ToList());

    private static BomLineDto MapLineToDto(BomLine l) => new(
        Id: l.Id,
        MaterialCode: l.MaterialCode,
        MaterialName: l.MaterialName,
        Quantity: l.Quantity,
        Unit: l.Unit,
        SubBomId: l.SubBomId,
        SortOrder: l.SortOrder,
        Notes: l.Notes);
}
