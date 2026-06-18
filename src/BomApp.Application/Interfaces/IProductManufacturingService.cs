using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces;

public interface IProductManufacturingService
{
    Task<Result<PagedResult<ProductManufacturingDto>>> GetDocumentsPageAsync(
        ProductManufacturingListQuery query,
        CancellationToken ct = default);

    Task<Result<ProductManufacturingDto>> GetDocumentByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<Result<ProductManufacturingDto>> CalculateAsync(
        CalculateProductManufacturingRequest request,
        CancellationToken ct = default);

    Task<Result<ProductManufacturingDto>> CreateAsync(
        CreateProductManufacturingCommand command,
        CancellationToken ct = default);

    Task<Result<ProductManufacturingDto>> UpdateAsync(
        string docNo,
        UpdateProductManufacturingCommand command,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        string docNo,
        CancellationToken ct = default);
}
