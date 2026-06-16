using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

public interface IProductDestructionRepository
{
    Task<PagedResult<ProductDestructionDto>> GetPageAsync(
        ProductDestructionListQuery query,
        CancellationToken ct = default);

    Task<ProductDestructionDto?> GetByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<ProductDestructionDto> CreateAsync(
        CreateProductDestructionCommand command,
        CancellationToken ct = default);

    Task<ProductDestructionDto?> UpdateAsync(
        string docNo,
        UpdateProductDestructionCommand command,
        CancellationToken ct = default);
}
