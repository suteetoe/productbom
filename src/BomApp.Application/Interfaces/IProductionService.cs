using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces;

/// <summary>
/// Service สำหรับจัดการ Production Orders — query และ lifecycle
/// </summary>
public interface IProductionService
{
    /// <summary>
    /// ดึงเอกสารผลิตจากรายการขายที่บันทึกไว้ใน bom_production_orders
    /// </summary>
    Task<Result<IReadOnlyList<BomProductionDto>>> GetDocumentsAsync(
        DateOnly? docDateFrom = null,
        DateOnly? docDateTo = null,
        string? docNo = null,
        string? itemCode = null,
        CancellationToken ct = default);

    /// <summary>
    /// ดึงเอกสารผลิตแบบแบ่งหน้า จาก bom_production_orders ตาม filter ที่ระบุ
    /// </summary>
    Task<Result<PagedResult<BomProductionDto>>> GetDocumentsPageAsync(
        BomProductionListQuery query,
        CancellationToken ct = default);

    /// <summary>ดึงเอกสารผลิตตามเลขที่เอกสาร</summary>
    Task<Result<BomProductionDto>> GetDocumentByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ดึงรายการขายที่ถูกบันทึกไว้ใน bom_production_orders ตามเลขที่เอกสารผลิต</summary>
    Task<Result<IReadOnlyList<BomProductionOrderDto>>> GetDocumentOrdersAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ดึงรายการสินค้าที่ต้องใช้จาก bom_production_details ตามเลขที่เอกสารผลิต</summary>
    Task<Result<IReadOnlyList<BomProductionDetailDto>>> GetDocumentDetailsAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ลบรายการขายใน bom_production_orders ตามเลขที่เอกสารผลิต</summary>
    Task<Result> DeleteDocumentAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>
    /// ดึง Production Orders ตาม filter ที่ระบุ
    /// </summary>
    Task<Result<IReadOnlyList<ProductionOrderDto>>> GetOrdersAsync(
        DateOnly?    dateFrom    = null,
        DateOnly?    dateTo      = null,
        string?      status      = null,
        string?      itemCode    = null,
        string?      createdVia  = null,   // "UI" | "CLI" | null = ทั้งหมด
        string?      sourceDocNo = null,   // ค้นหาใน source_so_numbers[]
        CancellationToken ct = default);

    /// <summary>ดึง Production Order ตาม Id</summary>
    Task<Result<ProductionOrderDto>> GetOrderByIdAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>ดึงรายการวัตถุดิบของ Production Order</summary>
    Task<Result<IReadOnlyList<ProductionOrderLineDto>>> GetOrderLinesAsync(
        Guid productionOrderId,
        CancellationToken ct = default);

    /// <summary>ยกเลิก Production Order — เฉพาะ status = Pending เท่านั้น</summary>
    Task<Result> CancelOrderAsync(
        CancelProductionOrderCommand cmd,
        CancellationToken ct = default);
}
