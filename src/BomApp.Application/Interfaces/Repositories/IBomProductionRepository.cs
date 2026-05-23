using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface สำหรับเอกสารเบิกรายการสินค้าที่ผลิต
/// อยู่ใน Application layer ตาม Clean Architecture
/// </summary>
public interface IBomProductionRepository
{
    /// <summary>สร้างเอกสาร bom_production พร้อม bom_production_detail</summary>
    Task<BomProductionDto> CreateAsync(
        CreateBomProductionInternalCommand cmd,
        CancellationToken ct = default);
}
