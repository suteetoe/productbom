using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface สำหรับ ERP Items — อ่านจาก erp-database (ic_inventory, ic_unit_use, ic_unit)
/// อยู่ใน Application layer ตาม Clean Architecture
/// Implementation อยู่ใน Infrastructure/Erp/ErpItemRepository.cs
/// </summary>
public interface IErpItemRepository
{
    /// <summary>ดึงสินค้าทั้งหมดจาก ic_inventory</summary>
    Task<IReadOnlyList<ErpItemDto>> GetAllItemsAsync(
        CancellationToken ct = default);

    /// <summary>ค้นหาสินค้าตาม code หรือ name_1</summary>
    Task<IReadOnlyList<ErpItemDto>> SearchItemsAsync(
        string keyword,
        CancellationToken ct = default);

    /// <summary>ดึงสินค้าตาม code — คืน null ถ้าไม่พบ</summary>
    Task<ErpItemDto?> GetItemByCodeAsync(
        string code,
        CancellationToken ct = default);

    /// <summary>ดึงหน่วยนับทั้งหมดของสินค้าจาก ic_unit_use JOIN ic_unit</summary>
    Task<IReadOnlyList<ErpUnitDto>> GetUnitsByItemCodeAsync(
        string icCode,
        CancellationToken ct = default);

    /// <summary>ดึง ic_unit ทั้งหมด (master)</summary>
    Task<IReadOnlyList<ErpUnitDto>> GetAllUnitsAsync(
        CancellationToken ct = default);
}
