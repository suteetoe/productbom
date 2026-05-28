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
}
