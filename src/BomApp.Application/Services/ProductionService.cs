using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Services;

/// <summary>
/// Implementation ของ IProductionService — query และ lifecycle ของ Production Orders
/// </summary>
public class ProductionService(
    IProductionOrderRepository productionOrderRepository,
    IBomProductionRepository bomProductionRepository) : IProductionService
{
    /// <summary>ดึงเอกสารผลิตจากรายการขายใน bom_production_orders ตาม filter</summary>
    public async Task<Result<IReadOnlyList<BomProductionDto>>> GetDocumentsAsync(
        DateOnly? docDateFrom = null,
        DateOnly? docDateTo = null,
        string? docNo = null,
        string? itemCode = null,
        CancellationToken ct = default)
    {
        var documents = await bomProductionRepository.GetAllAsync(
            docDateFrom, docDateTo, docNo, itemCode, ct);
        return Result<IReadOnlyList<BomProductionDto>>.Success(documents);
    }

    /// <summary>ดึงเอกสารผลิตตามเลขที่เอกสาร</summary>
    public async Task<Result<BomProductionDto>> GetDocumentByDocNoAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var document = await bomProductionRepository.GetByDocNoAsync(docNo, ct);
        if (document is null)
            return Result<BomProductionDto>.Failure($"ไม่พบเอกสารผลิตเลขที่: {docNo}");

        return Result<BomProductionDto>.Success(document);
    }

    /// <summary>ดึงรายการขายใน bom_production_orders ตามเลขที่เอกสาร</summary>
    public async Task<Result<IReadOnlyList<BomProductionOrderDto>>> GetDocumentOrdersAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var document = await bomProductionRepository.GetByDocNoAsync(docNo, ct);
        if (document is null)
            return Result<IReadOnlyList<BomProductionOrderDto>>.Failure(
                $"ไม่พบเอกสารผลิตเลขที่: {docNo}");

        var orders = await bomProductionRepository.GetOrdersByDocNoAsync(docNo, ct);
        return Result<IReadOnlyList<BomProductionOrderDto>>.Success(orders);
    }

    /// <summary>ดึงรายการสินค้าที่ต้องใช้ใน bom_production_details ตามเลขที่เอกสาร</summary>
    public async Task<Result<IReadOnlyList<BomProductionDetailDto>>> GetDocumentDetailsAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var document = await bomProductionRepository.GetByDocNoAsync(docNo, ct);
        if (document is null)
            return Result<IReadOnlyList<BomProductionDetailDto>>.Failure(
                $"ไม่พบเอกสารผลิตเลขที่: {docNo}");

        var details = await bomProductionRepository.GetDetailsByDocNoAsync(docNo, ct);
        return Result<IReadOnlyList<BomProductionDetailDto>>.Success(details);
    }

    /// <summary>ลบรายการขายใน bom_production_orders ตามเลขที่เอกสารผลิต</summary>
    public async Task<Result> DeleteDocumentAsync(string docNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(docNo))
            return Result.Failure("กรุณาระบุเลขที่เอกสารผลิตที่ต้องการลบ");

        var deleted = await bomProductionRepository.DeleteByDocNoAsync(docNo, ct);
        if (!deleted)
            return Result.Failure($"ไม่พบเอกสารผลิตเลขที่: {docNo}");

        return Result.Success();
    }

    /// <summary>ดึง Production Orders ตาม filter</summary>
    public async Task<Result<IReadOnlyList<ProductionOrderDto>>> GetOrdersAsync(
        DateOnly?    dateFrom    = null,
        DateOnly?    dateTo      = null,
        string?      status      = null,
        string?      itemCode    = null,
        string?      createdVia  = null,
        string?      sourceDocNo = null,
        CancellationToken ct = default)
    {
        var orders = await productionOrderRepository.GetAllAsync(
            dateFrom, dateTo, status, itemCode, createdVia, sourceDocNo, ct);
        return Result<IReadOnlyList<ProductionOrderDto>>.Success(orders);
    }

    /// <summary>ดึง Production Order ตาม Id</summary>
    public async Task<Result<ProductionOrderDto>> GetOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await productionOrderRepository.GetByIdAsync(id, ct);
        if (order is null)
            return Result<ProductionOrderDto>.Failure($"ไม่พบ Production Order Id: {id}");
        return Result<ProductionOrderDto>.Success(order);
    }

    /// <summary>ดึงรายการวัตถุดิบของ Production Order</summary>
    public async Task<Result<IReadOnlyList<ProductionOrderLineDto>>> GetOrderLinesAsync(
        Guid productionOrderId,
        CancellationToken ct = default)
    {
        var order = await productionOrderRepository.GetByIdAsync(productionOrderId, ct);
        if (order is null)
            return Result<IReadOnlyList<ProductionOrderLineDto>>.Failure(
                $"ไม่พบ Production Order Id: {productionOrderId}");

        var lines = await productionOrderRepository.GetLinesByOrderIdAsync(productionOrderId, ct);
        return Result<IReadOnlyList<ProductionOrderLineDto>>.Success(lines);
    }

    /// <summary>
    /// ยกเลิก Production Order
    /// Business rule: เฉพาะ status = Pending เท่านั้น
    /// </summary>
    public async Task<Result> CancelOrderAsync(CancelProductionOrderCommand cmd, CancellationToken ct = default)
    {
        var order = await productionOrderRepository.GetByIdAsync(cmd.OrderId, ct);
        if (order is null)
            return Result.Failure($"ไม่พบ Production Order Id: {cmd.OrderId}");

        if (order.Status != "Pending")
            return Result.Failure($"ไม่สามารถยกเลิก Production Order ที่มีสถานะ '{order.Status}' ได้ (ต้องเป็น Pending เท่านั้น)");

        await productionOrderRepository.SetStatusAsync(cmd.OrderId, "Cancelled", ct);
        return Result.Success();
    }
}
