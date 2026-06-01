using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Services;

/// <summary>
/// Implementation ของ IBomService — จัดการ BOM lifecycle พร้อม business rules
/// </summary>
public class BomService(IBomRepository bomRepository) : IBomService
{
    /// <summary>ดึง BOM ทั้งหมดในระบบ</summary>
    public async Task<Result<IReadOnlyList<BomDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var boms = await bomRepository.GetAllAsync(ct);
        return Result<IReadOnlyList<BomDto>>.Success(boms);
    }

    /// <summary>ดึง BOM แบบแบ่งหน้า พร้อมค้นหาจาก code/name</summary>
    public async Task<Result<PagedResult<BomDto>>> GetPageAsync(BomListQuery query, CancellationToken ct = default)
    {
        if (query.PageNumber < 1)
            return Result<PagedResult<BomDto>>.Failure("เลขหน้าต้องมากกว่าหรือเท่ากับ 1");

        if (query.PageSize < 1)
            return Result<PagedResult<BomDto>>.Failure("จำนวนรายการต่อหน้าต้องมากกว่าหรือเท่ากับ 1");

        var page = await bomRepository.GetPageAsync(query, ct);
        return Result<PagedResult<BomDto>>.Success(page);
    }

    /// <summary>ดึง BOM ตาม Id</summary>
    public async Task<Result<BomDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var bom = await bomRepository.GetByIdAsync(id, ct);
        if (bom is null)
            return Result<BomDto>.Failure($"ไม่พบ BOM Id: {id}");
        return Result<BomDto>.Success(bom);
    }

    /// <summary>ดึง BOM ตาม Code</summary>
    public async Task<Result<BomDto>> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var bom = await bomRepository.GetByCodeAsync(code, ct);
        if (bom is null)
            return Result<BomDto>.Failure($"ไม่พบ BOM Code: {code}");
        return Result<BomDto>.Success(bom);
    }

    /// <summary>
    /// สร้าง BOM ใหม่
    /// Business rules: ตรวจสอบ Code uniqueness และ circular reference ใน sub-BOM lines
    /// </summary>
    public async Task<Result<BomDto>> CreateAsync(CreateBomCommand cmd, CancellationToken ct = default)
    {
        // Rule: BOM Code ต้อง unique ทั้งระบบ
        var codeExists = await bomRepository.ExistsCodeAsync(cmd.Code, excludeId: null, ct);
        if (codeExists)
            return Result<BomDto>.Failure("รหัส BOM ซ้ำ");

        // Rule: ตรวจ circular reference ใน lines ที่มี SubBomId
        var subBomIds = cmd.Lines
            .Where(l => l.SubBomId.HasValue)
            .Select(l => l.SubBomId!.Value)
            .ToList();

        foreach (var subBomId in subBomIds)
        {
            // ตรวจว่า sub-BOM มี Code เดียวกับ BOM ที่กำลังสร้าง (self-reference by code)
            var subBom = await bomRepository.GetByIdAsync(subBomId, ct);
            if (subBom is not null && subBom.Code == cmd.Code)
                return Result<BomDto>.Failure($"พบ circular reference (วนซ้ำ): SubBom Id: {subBomId} มี Code เดียวกับ BOM ที่กำลังสร้าง");

            var circularResult = await CheckCircularReferenceAsync(subBomId, new HashSet<Guid>(), ct);
            if (circularResult)
                return Result<BomDto>.Failure($"พบ circular reference ใน SubBom Id: {subBomId}");
        }

        // createdBy: ใช้ placeholder — production จะมาจาก session/context
        var created = await bomRepository.CreateAsync(cmd, createdBy: "SYSTEM", ct);
        return Result<BomDto>.Success(created);
    }

    /// <summary>
    /// แก้ไข BOM
    /// Business rules:
    /// - ถ้า BOM status = Active ห้ามแก้ไข Lines, ItemCode, YieldQuantity, YieldUnit (แก้ได้เฉพาะ Name/Description)
    /// - ตรวจสอบ circular reference ใน sub-BOM lines ใหม่
    /// - version จะถูก bump โดย repository ทุกครั้งที่ update
    /// </summary>
    public async Task<Result<BomDto>> UpdateAsync(Guid id, UpdateBomCommand cmd, CancellationToken ct = default)
    {
        var existing = await bomRepository.GetByIdAsync(id, ct);
        if (existing is null)
            return Result<BomDto>.Failure($"ไม่พบ BOM Id: {id}");

        // Rule: BOM ที่ Active ห้ามแก้ไข Lines, ItemCode, YieldQuantity, YieldUnit
        if (existing.Status == "Active")
        {
            bool hasStructuralChange =
                cmd.Lines is not null ||
                (cmd.ItemCode is not null && cmd.ItemCode != existing.ItemCode) ||
                (cmd.YieldQuantity.HasValue && cmd.YieldQuantity.Value != existing.YieldQuantity) ||
                (cmd.YieldUnit is not null && cmd.YieldUnit != existing.YieldUnit);

            if (hasStructuralChange)
                return Result<BomDto>.Failure(
                    "ไม่สามารถแก้ไขโครงสร้าง BOM ที่ Active ได้ — กรุณา Deactivate ก่อน หรือแก้ไขเฉพาะ Name/Description");
        }

        // ตรวจ circular reference ถ้ามี lines ใหม่
        if (cmd.Lines is not null)
        {
            var subBomIds = cmd.Lines
                .Where(l => l.SubBomId.HasValue)
                .Select(l => l.SubBomId!.Value)
                .ToList();

            foreach (var subBomId in subBomIds)
            {
                // ห้าม sub-BOM ชี้กลับมาที่ตัวเอง
                var visited = new HashSet<Guid> { id };
                var circularResult = await CheckCircularReferenceAsync(subBomId, visited, ct);
                if (circularResult)
                    return Result<BomDto>.Failure($"พบ circular reference ใน SubBom Id: {subBomId}");
            }
        }

        // version bump และ updated_at ดำเนินการใน repository
        var updated = await bomRepository.UpdateAsync(id, cmd, ct);
        return Result<BomDto>.Success(updated);
    }

    /// <summary>
    /// เปลี่ยนสถานะ BOM เป็น Active
    /// Business rule: ต้องมี BOM line อย่างน้อย 1 รายการ
    /// </summary>
    public async Task<Result> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var bom = await bomRepository.GetByIdAsync(id, ct);
        if (bom is null)
            return Result.Failure($"ไม่พบ BOM Id: {id}");

        if (bom.Lines.Count == 0)
            return Result.Failure("ต้องมีรายการวัตถุดิบอย่างน้อย 1 รายการ");

        await bomRepository.SetStatusAsync(id, "Active", ct);
        return Result.Success();
    }

    /// <summary>เปลี่ยนสถานะ BOM เป็น Inactive</summary>
    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var bom = await bomRepository.GetByIdAsync(id, ct);
        if (bom is null)
            return Result.Failure($"ไม่พบ BOM Id: {id}");

        await bomRepository.SetStatusAsync(id, "Inactive", ct);
        return Result.Success();
    }

    /// <summary>
    /// ลบ BOM
    /// Business rule: ห้ามลบ BOM ที่สถานะ Active
    /// </summary>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var bom = await bomRepository.GetByIdAsync(id, ct);
        if (bom is null)
            return Result.Failure($"ไม่พบ BOM Id: {id}");

        if (bom.Status == "Active")
            return Result.Failure("ไม่สามารถลบ BOM ที่ Active ได้");

        await bomRepository.DeleteAsync(id, ct);
        return Result.Success();
    }

    /// <summary>
    /// ตรวจสอบ circular reference ด้วย DFS
    /// คืน true ถ้าพบ circular reference
    /// </summary>
    private async Task<bool> CheckCircularReferenceAsync(
        Guid bomId,
        HashSet<Guid> visited,
        CancellationToken ct)
    {
        if (!visited.Add(bomId))
            return true; // พบ circular reference

        var bom = await bomRepository.GetByIdAsync(bomId, ct);
        if (bom is null)
            return false;

        foreach (var line in bom.Lines.Where(l => l.SubBomId.HasValue))
        {
            var subResult = await CheckCircularReferenceAsync(line.SubBomId!.Value, visited, ct);
            if (subResult) return true;
        }

        visited.Remove(bomId);
        return false;
    }
}
