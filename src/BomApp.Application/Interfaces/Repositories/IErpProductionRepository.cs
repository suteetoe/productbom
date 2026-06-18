using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Writes processed production documents back to the ERP database.
/// ERP table and column names must stay inside the Infrastructure implementation.
/// </summary>
public interface IErpProductionRepository
{
    Task SaveProductionDocumentAsync(
        BomProductionDto document,
        CancellationToken ct = default);

    Task SaveProductDestructionDocumentAsync(
        ProductDestructionDto document,
        CancellationToken ct = default);

    Task SaveProductManufacturingDocumentAsync(
        ProductManufacturingDto document,
        CancellationToken ct = default);

    Task DeleteProductionDocumentAsync(
        string docNo,
        CancellationToken ct = default);

    Task DeleteProductManufacturingDocumentAsync(
        string docNo,
        CancellationToken ct = default);

    Task DeleteProductDestructionDocumentAsync(
        string docNo,
        CancellationToken ct = default);
}
