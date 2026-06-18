using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

public interface IProductManufacturingRepository
{
    Task<PagedResult<ProductManufacturingDto>> GetPageAsync(
        ProductManufacturingListQuery query,
        CancellationToken ct = default);

    Task<ProductManufacturingDto?> GetByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<ProductManufacturingDto> CreateAsync(
        CreateProductManufacturingCommand command,
        CancellationToken ct = default);

    Task<ProductManufacturingDto?> UpdateAsync(
        string docNo,
        UpdateProductManufacturingCommand command,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(
        string docNo,
        CancellationToken ct = default);
}
