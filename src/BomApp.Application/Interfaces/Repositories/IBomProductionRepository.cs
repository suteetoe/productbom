using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface สำหรับเอกสารเบิกรายการสินค้าที่ผลิต
/// อยู่ใน Application layer ตาม Clean Architecture
/// </summary>
public interface IBomProductionRepository
{
    /// <summary>ดึงเอกสารผลิตจากรายการขายที่บันทึกไว้ใน bom_production_orders ตาม filter ที่ระบุ</summary>
    Task<IReadOnlyList<BomProductionDto>> GetAllAsync(
        DateOnly? docDateFrom = null,
        DateOnly? docDateTo = null,
        string? docNo = null,
        string? itemCode = null,
        CancellationToken ct = default);

    /// <summary>ดึงเอกสารผลิตแบบแบ่งหน้า ตาม filter ที่ระบุ</summary>
    Task<PagedResult<BomProductionDto>> GetPageAsync(
        BomProductionListQuery query,
        CancellationToken ct = default);

    /// <summary>ดึงเอกสารผลิตตามเลขที่เอกสาร</summary>
    Task<BomProductionDto?> GetByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ดึงรายการขายใน bom_production_orders ตามเลขที่เอกสารผลิต</summary>
    Task<IReadOnlyList<BomProductionOrderDto>> GetOrdersByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ดึงรายการสินค้าที่ต้องใช้ใน bom_production_details ตามเลขที่เอกสารผลิต</summary>
    Task<IReadOnlyList<BomProductionDetailDto>> GetDetailsByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ลบรายการขายใน bom_production_orders ตามเลขที่เอกสารผลิต</summary>
    Task<bool> DeleteByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>สร้างรายการขายที่เลือกไว้ใน bom_production_orders</summary>
    Task<BomProductionDto> CreateAsync(
        CreateBomProductionInternalCommand cmd,
        CancellationToken ct = default);
}
