using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface สำหรับ BOM — อยู่ใน Application layer ตาม Clean Architecture
/// Implementation อยู่ใน Infrastructure layer
/// </summary>
public interface IBomRepository
{
    /// <summary>ดึง BOM ทั้งหมด</summary>
    Task<IReadOnlyList<BomDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>ดึง BOM แบบแบ่งหน้า พร้อมค้นหาจาก code/name</summary>
    Task<PagedResult<BomDto>> GetPageAsync(BomListQuery query, CancellationToken ct = default);

    /// <summary>ดึง BOM ตาม Id — คืน null ถ้าไม่พบ</summary>
    Task<BomDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>ดึง BOM ตาม Code — คืน null ถ้าไม่พบ</summary>
    Task<BomDto?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>ตรวจสอบว่า Code ซ้ำหรือไม่ — excludeId ใช้ตอน update</summary>
    Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>สร้าง BOM ใหม่</summary>
    Task<BomDto> CreateAsync(CreateBomCommand cmd, string createdBy, CancellationToken ct = default);

    /// <summary>แก้ไข BOM</summary>
    Task<BomDto> UpdateAsync(Guid id, UpdateBomCommand cmd, CancellationToken ct = default);

    /// <summary>ลบ BOM</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>เปลี่ยน status ของ BOM</summary>
    Task SetStatusAsync(Guid id, string status, CancellationToken ct = default);
}
