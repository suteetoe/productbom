using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation ของ IBomAssignmentRepository — ใช้ BomDbContext
/// </summary>
public class BomAssignmentRepository(BomDbContext context) : IBomAssignmentRepository
{
    /// <summary>ค้นหา BOM Id จาก item_code — คืน null ถ้าไม่มี assignment</summary>
    public async Task<Guid?> GetBomIdByItemCodeAsync(string itemCode, CancellationToken ct = default)
    {
        var assignment = await context.BomAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ItemCode == itemCode, ct);

        return assignment?.BomId;
    }

    /// <summary>ดึง mapping ระหว่าง item_code และ BOM Id สำหรับ items หลายรายการ</summary>
    public async Task<IReadOnlyDictionary<string, Guid>> GetAssignedItemCodesAsync(
        IReadOnlyList<string> itemCodes,
        CancellationToken ct = default)
    {
        var assignments = await context.BomAssignments
            .AsNoTracking()
            .Where(a => itemCodes.Contains(a.ItemCode))
            .Select(a => new { a.ItemCode, a.BomId })
            .ToListAsync(ct);

        return assignments.ToDictionary(a => a.ItemCode, a => a.BomId);
    }

    /// <summary>ผูก BOM กับ item_code — upsert (override ถ้ามีอยู่แล้ว)</summary>
    public async Task AssignAsync(
        string itemCode,
        string itemName,
        Guid bomId,
        string assignedBy,
        CancellationToken ct = default)
    {
        var existing = await context.BomAssignments
            .FirstOrDefaultAsync(a => a.ItemCode == itemCode, ct);

        if (existing is not null)
        {
            existing.BomId = bomId;
            existing.ItemName = itemName;
            existing.AssignedAt = DateTime.UtcNow;
            existing.AssignedBy = assignedBy;
        }
        else
        {
            context.BomAssignments.Add(new BomAssignment
            {
                Id = Guid.NewGuid(),
                ItemCode = itemCode,
                ItemName = itemName,
                BomId = bomId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy
            });
        }

        await context.SaveChangesAsync(ct);
    }

    /// <summary>ถอด assignment ออกจาก item_code</summary>
    public async Task RemoveAsync(string itemCode, CancellationToken ct = default)
    {
        await context.BomAssignments
            .Where(a => a.ItemCode == itemCode)
            .ExecuteDeleteAsync(ct);
    }
}
