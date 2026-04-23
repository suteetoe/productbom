using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces;

/// <summary>
/// Service สำหรับจัดการ BOM — CRUD และ lifecycle operations
/// </summary>
public interface IBomService
{
    /// <summary>ดึง BOM ทั้งหมดในระบบ</summary>
    Task<Result<IReadOnlyList<BomDto>>> GetAllAsync(
        CancellationToken ct = default);

    /// <summary>ดึง BOM ตาม Id</summary>
    Task<Result<BomDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>ดึง BOM ตาม Code</summary>
    Task<Result<BomDto>> GetByCodeAsync(
        string code,
        CancellationToken ct = default);

    /// <summary>สร้าง BOM ใหม่ — ตรวจสอบ Code uniqueness ก่อน</summary>
    Task<Result<BomDto>> CreateAsync(
        CreateBomCommand cmd,
        CancellationToken ct = default);

    /// <summary>แก้ไข BOM — เพิ่ม version อัตโนมัติ</summary>
    Task<Result<BomDto>> UpdateAsync(
        Guid id,
        UpdateBomCommand cmd,
        CancellationToken ct = default);

    /// <summary>เปลี่ยนสถานะ BOM เป็น Active — ต้องมี line อย่างน้อย 1 รายการ</summary>
    Task<Result> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>เปลี่ยนสถานะ BOM เป็น Inactive</summary>
    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>ลบ BOM — ห้ามลบ BOM ที่สถานะ Active</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
