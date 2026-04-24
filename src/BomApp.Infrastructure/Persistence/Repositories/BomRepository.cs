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

    /// <summary>แก้ไข BOM — replace lines และเพิ่ม version
    /// ใช้ ExecuteUpdateAsync + ExecuteDeleteAsync ทั้งหมด เพื่อหลีกเลี่ยง change-tracker
    /// conflict (DbUpdateConcurrencyException) เมื่อ DbContext มีอายุยาวใน Avalonia root scope
    /// </summary>
    public async Task<BomDto> UpdateAsync(Guid id, UpdateBomCommand cmd, CancellationToken ct = default)
    {
        // Load current values without entering the change tracker
        var current = await context.Boms
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new InvalidOperationException($"BOM {id} not found");

        // Update header fields directly in DB — no change tracker involved
        await context.Boms
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Name,          cmd.Name          ?? current.Name)
                .SetProperty(b => b.Description,   cmd.Description   ?? current.Description)
                .SetProperty(b => b.ItemCode,      cmd.ItemCode      ?? current.ItemCode)
                .SetProperty(b => b.YieldQuantity, cmd.YieldQuantity ?? current.YieldQuantity)
                .SetProperty(b => b.YieldUnit,     cmd.YieldUnit     ?? current.YieldUnit)
                .SetProperty(b => b.Version,       current.Version + 1)
                .SetProperty(b => b.UpdatedAt,     DateTime.UtcNow), ct);

        // Replace lines if provided — ExecuteDeleteAsync issues a single DELETE statement
        // that bypasses the change tracker, so no second DELETE is triggered by SaveChanges
        if (cmd.Lines is not null)
        {
            await context.BomLines
                .Where(l => l.BomId == id)
                .ExecuteDeleteAsync(ct);

            if (cmd.Lines.Count > 0)
            {
                var newLines = cmd.Lines.Select(l => new BomLine
                {
                    Id           = Guid.NewGuid(),
                    BomId        = id,
                    MaterialCode = l.MaterialCode,
                    MaterialName = string.Empty,
                    Quantity     = l.Quantity,
                    Unit         = l.Unit,
                    SubBomId     = l.SubBomId,
                    SortOrder    = l.SortOrder,
                    Notes        = l.Notes
                }).ToList();

                context.BomLines.AddRange(newLines);
                await context.SaveChangesAsync(ct);
            }
        }

        // Return a fresh DTO read via AsNoTracking — change tracker is untouched throughout
        return (await GetByIdAsync(id, ct))!;
    }

    /// <summary>ลบ BOM และ lines ทั้งหมด — ใช้ ExecuteDeleteAsync เพื่อหลีกเลี่ยง change-tracker
    /// conflict เมื่อ DbContext มีอายุยาว (resolved จาก root scope)</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // Verify existence first so callers get a meaningful exception on missing BOM.
        var exists = await context.Boms.AsNoTracking().AnyAsync(b => b.Id == id, ct);
        if (!exists)
            throw new InvalidOperationException($"BOM {id} not found");

        // ExecuteDeleteAsync issues a single DELETE statement and bypasses the
        // change tracker entirely.  Lines are removed by the ON DELETE CASCADE
        // constraint configured in BomConfiguration, so no separate line delete
        // is needed.  This avoids DbUpdateConcurrencyException when a previously
        // tracked Bom or its Lines are still held in the change tracker.
        await context.Boms
            .Where(b => b.Id == id)
            .ExecuteDeleteAsync(ct);
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
