using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface สำหรับเอกสารเบิกรายการสินค้าที่ผลิต
/// อยู่ใน Application layer ตาม Clean Architecture
/// </summary>
public interface IBomProductionRepository
{
    /// <summary>ดึงเอกสาร bom_production ตาม filter ที่ระบุ</summary>
    Task<IReadOnlyList<BomProductionDto>> GetAllAsync(
        DateOnly? docDateFrom = null,
        DateOnly? docDateTo = null,
        string? docNo = null,
        string? itemCode = null,
        CancellationToken ct = default);

    /// <summary>ดึงเอกสาร bom_production ตามเลขที่เอกสาร</summary>
    Task<BomProductionDto?> GetByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ดึงรายการสินค้าใน bom_production_detail ตามเลขที่เอกสาร</summary>
    Task<IReadOnlyList<BomProductionDetailDto>> GetDetailsByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ลบเอกสาร bom_production ตามเลขที่เอกสาร</summary>
    Task<bool> DeleteByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>สร้างเอกสาร bom_production พร้อม bom_production_detail</summary>
    Task<BomProductionDto> CreateAsync(
        CreateBomProductionInternalCommand cmd,
        CancellationToken ct = default);
}
