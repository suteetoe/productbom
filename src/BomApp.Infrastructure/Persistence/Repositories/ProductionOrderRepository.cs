using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;

namespace BomApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Legacy production-order repository kept for the older interface contract.
/// The active sales-calculation persistence now uses IBomProductionRepository
/// and public.bom_production_orders.
/// </summary>
public class ProductionOrderRepository : IProductionOrderRepository
{
    public Task<IReadOnlyList<ProductionOrderDto>> GetAllAsync(
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        string? status = null,
        string? itemCode = null,
        string? createdVia = null,
        string? sourceDocNo = null,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProductionOrderDto>>([]);

    public Task<ProductionOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<ProductionOrderDto?>(null);

    public Task<IReadOnlyList<ProductionOrderLineDto>> GetLinesByOrderIdAsync(
        Guid productionOrderId,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProductionOrderLineDto>>([]);

    public Task<IReadOnlyList<string>> GetAlreadyProcessedDocNosAsync(
        IReadOnlyList<string> docNos,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<ProductionOrderDto> CreateAsync(
        CreateProductionOrderInternalCommand cmd,
        CancellationToken ct = default) =>
        throw new NotSupportedException(
            "Legacy production orders are replaced by bom_production_orders selected-sales rows.");

    public Task SetStatusAsync(Guid id, string status, CancellationToken ct = default) =>
        Task.CompletedTask;
}
