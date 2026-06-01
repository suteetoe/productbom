namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Requests ERP to process stock documents after production issue documents are saved.
/// ERP endpoint details must stay inside the Infrastructure implementation.
/// </summary>
public interface IErpStockRequestProcessor
{
    Task ProcessStockRequestAsync(
        IReadOnlyList<string> itemCodes,
        CancellationToken ct = default);
}
