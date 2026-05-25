using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces;

/// <summary>
/// Service สำหรับจัดการ Production Orders — query และ lifecycle
/// </summary>
public interface IProductionService
{
    /// <summary>
    /// ดึงเอกสารเบิกรายการสินค้าที่ผลิตจาก bom_production
    /// </summary>
    Task<Result<IReadOnlyList<BomProductionDto>>> GetDocumentsAsync(
        DateOnly? docDateFrom = null,
        DateOnly? docDateTo = null,
        string? docNo = null,
        string? itemCode = null,
        CancellationToken ct = default);

    /// <summary>ดึงเอกสาร bom_production ตามเลขที่เอกสาร</summary>
    Task<Result<BomProductionDto>> GetDocumentByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ดึงรายละเอียดจาก bom_production_detail ตามเลขที่เอกสาร</summary>
    Task<Result<IReadOnlyList<BomProductionDetailDto>>> GetDocumentDetailsAsync(
        string docNo,
        CancellationToken ct = default);

    /// <summary>ลบเอกสาร bom_production และรายละเอียด bom_production_detail</summary>
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
