using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Services;

/// <summary>
/// Implementation ของ IBomAssignmentService — ผูก/ถอด BOM กับ Product Item จาก ERP
/// </summary>
public class BomAssignmentService(
    IBomRepository bomRepository,
    IBomAssignmentRepository assignmentRepository) : IBomAssignmentService
{
    /// <summary>คืน BOM ทั้งหมดที่มีสถานะ Active — ใช้สำหรับ dropdown selector</summary>
    public async Task<Result<IReadOnlyList<BomDto>>> GetActiveBomsAsync(CancellationToken ct = default)
    {
        var all = await bomRepository.GetAllAsync(ct);
        var active = all.Where(b => b.Status == "Active").ToList();
        return Result<IReadOnlyList<BomDto>>.Success(active);
    }

    /// <summary>คืน BOM ที่ assign กับ itemCode อยู่ หรือ null ถ้าไม่มี</summary>
    public async Task<Result<BomDto?>> GetAssignedBomAsync(string itemCode, CancellationToken ct = default)
    {
        var bomId = await assignmentRepository.GetBomIdByItemCodeAsync(itemCode, ct);
        if (bomId is null)
            return Result<BomDto?>.Success(null);

        var bom = await bomRepository.GetByIdAsync(bomId.Value, ct);
        return Result<BomDto?>.Success(bom);
    }

    /// <summary>
    /// ผูก bomId กับ itemCode — ตรวจสอบว่า BOM status = Active ก่อน assign
    /// Override assignment เดิม (UPSERT)
    /// </summary>
    public async Task<Result> AssignAsync(
        string itemCode,
        string itemName,
        Guid bomId,
        string assignedBy,
        CancellationToken ct = default)
    {
        var bom = await bomRepository.GetByIdAsync(bomId, ct);
        if (bom is null)
            return Result.Failure($"ไม่พบ BOM Id: {bomId}");
        if (bom.Status != "Active")
            return Result.Failure("ไม่สามารถ assign BOM ที่ไม่ได้อยู่ในสถานะ Active ได้");

        await assignmentRepository.AssignAsync(itemCode, itemName, bomId, assignedBy, ct);
        return Result.Success();
    }

    /// <summary>ถอด BOM assignment ออกจาก itemCode — ไม่มีผลถ้าไม่มี assignment</summary>
    public async Task<Result> RemoveAsync(string itemCode, CancellationToken ct = default)
    {
        await assignmentRepository.RemoveAsync(itemCode, ct);
        return Result.Success();
    }
}
