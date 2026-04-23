namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface สำหรับ BOM Assignment — เชื่อม Product Item กับ BOM
/// อยู่ใน Application layer ตาม Clean Architecture
/// </summary>
public interface IBomAssignmentRepository
{
    /// <summary>ค้นหา BOM Id จาก item_code — คืน null ถ้าไม่มี assignment</summary>
    Task<Guid?> GetBomIdByItemCodeAsync(string itemCode, CancellationToken ct = default);

    /// <summary>
    /// ดึง mapping ระหว่าง item_code และ BOM Id สำหรับ item หลายรายการพร้อมกัน
    /// ใช้เพื่อ filter สินค้าที่มี BOM assign แล้ว
    /// </summary>
    Task<IReadOnlyDictionary<string, Guid>> GetAssignedItemCodesAsync(
        IReadOnlyList<string> itemCodes,
        CancellationToken ct = default);

    /// <summary>ผูก BOM กับ item_code — ถ้า assign ใหม่จะ override ของเดิม</summary>
    Task AssignAsync(string itemCode, string itemName, Guid bomId, string assignedBy, CancellationToken ct = default);

    /// <summary>ถอด assignment ออกจาก item_code</summary>
    Task RemoveAsync(string itemCode, CancellationToken ct = default);
}
