using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Services;

/// <summary>
/// Implementation ของ IProductionService — query และ lifecycle ของ Production Orders
/// </summary>
public class ProductionService(IProductionOrderRepository productionOrderRepository) : IProductionService
{
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
