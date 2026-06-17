using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces;

public interface IProductDestructionService
{
    Task<Result<PagedResult<ProductDestructionDto>>> GetDocumentsPageAsync(
        ProductDestructionListQuery query,
        CancellationToken ct = default);

    Task<Result<ProductDestructionDto>> GetDocumentByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<Result<ProductDestructionDto>> CreateAsync(
        CreateProductDestructionCommand command,
        CancellationToken ct = default);

    Task<Result<ProductDestructionDto>> UpdateAsync(
        string docNo,
        UpdateProductDestructionCommand command,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        string docNo,
        CancellationToken ct = default);
}
