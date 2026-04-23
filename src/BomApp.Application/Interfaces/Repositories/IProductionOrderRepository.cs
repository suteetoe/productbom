using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface สำหรับ Production Orders
/// อยู่ใน Application layer ตาม Clean Architecture
/// </summary>
public interface IProductionOrderRepository
{
    /// <summary>ดึง Production Orders ตาม filter ที่ระบุ</summary>
    Task<IReadOnlyList<ProductionOrderDto>> GetAllAsync(
        DateOnly?    dateFrom    = null,
        DateOnly?    dateTo      = null,
        string?      status      = null,
        string?      itemCode    = null,
        string?      createdVia  = null,
        string?      sourceDocNo = null,
        CancellationToken ct = default);

    /// <summary>ดึง Production Order ตาม Id — คืน null ถ้าไม่พบ</summary>
    Task<ProductionOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>ดึงรายการวัตถุดิบของ Production Order</summary>
    Task<IReadOnlyList<ProductionOrderLineDto>> GetLinesByOrderIdAsync(
        Guid productionOrderId,
        CancellationToken ct = default);

    /// <summary>ตรวจว่า doc_no เหล่านี้มีใน source_so_numbers ของ order ที่มีอยู่แล้วหรือไม่</summary>
    Task<IReadOnlyList<string>> GetAlreadyProcessedDocNosAsync(
        IReadOnlyList<string> docNos,
        CancellationToken ct = default);

    /// <summary>สร้าง Production Order ใหม่</summary>
    Task<ProductionOrderDto> CreateAsync(
        CreateProductionOrderInternalCommand cmd,
        CancellationToken ct = default);

    /// <summary>เปลี่ยน status ของ Production Order</summary>
    Task SetStatusAsync(Guid id, string status, CancellationToken ct = default);
}
