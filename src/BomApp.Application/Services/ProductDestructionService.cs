using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Services;

public class ProductDestructionService(
    IProductDestructionRepository repository,
    IErpItemRepository erpItemRepository,
    IErpProductionRepository erpProductionRepository,
    IErpStockRequestProcessor erpStockRequestProcessor) : IProductDestructionService
{
    public async Task<Result<PagedResult<ProductDestructionDto>>> GetDocumentsPageAsync(
        ProductDestructionListQuery query,
        CancellationToken ct = default)
    {
        if (query.PageNumber < 1)
            return Result<PagedResult<ProductDestructionDto>>.Failure("Page number must be at least 1.");

        if (query.PageSize < 1)
            return Result<PagedResult<ProductDestructionDto>>.Failure("Page size must be at least 1.");

        var page = await repository.GetPageAsync(query, ct);
        return Result<PagedResult<ProductDestructionDto>>.Success(page);
    }

    public async Task<Result<ProductDestructionDto>> GetDocumentByDocNoAsync(
        string docNo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(docNo))
            return Result<ProductDestructionDto>.Failure("Document number is required.");

        var document = await repository.GetByDocNoAsync(docNo.Trim(), ct);
        if (document is null)
            return Result<ProductDestructionDto>.Failure($"Product destruction document not found: {docNo}");

        return Result<ProductDestructionDto>.Success(await HydrateItemNamesAsync(document, ct));
    }

    public async Task<Result<ProductDestructionDto>> CreateAsync(
        CreateProductDestructionCommand command,
        CancellationToken ct = default)
    {
        var validation = Validate(command.DocNo, command.WhCode, command.ShelfCode, command.Details);
        if (validation is not null)
            return Result<ProductDestructionDto>.Failure(validation);

        var pictureValidation = ValidatePictures(command.Pictures);
        if (pictureValidation is not null)
            return Result<ProductDestructionDto>.Failure(pictureValidation);

        var existing = await repository.GetByDocNoAsync(command.DocNo.Trim(), ct);
        if (existing is not null)
            return Result<ProductDestructionDto>.Failure($"Document number already exists: {command.DocNo}");

        try
        {
            var document = await repository.CreateAsync(command, ct);
            var hydrated = await HydrateItemNamesAsync(document, ct);
            await erpProductionRepository.SaveProductDestructionDocumentAsync(hydrated, ct);
            await erpStockRequestProcessor.ProcessStockRequestAsync(GetStockProcessItemCodes(hydrated), ct);
            return Result<ProductDestructionDto>.Success(hydrated);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<ProductDestructionDto>.Failure($"Failed to save product destruction to ERP: {ex.Message}");
        }
    }

    public async Task<Result<ProductDestructionDto>> UpdateAsync(
        string docNo,
        UpdateProductDestructionCommand command,
        CancellationToken ct = default)
    {
        var validation = Validate(docNo, command.WhCode, command.ShelfCode, command.Details);
        if (validation is not null)
            return Result<ProductDestructionDto>.Failure(validation);

        var pictureValidation = ValidatePictures(command.Pictures);
        if (pictureValidation is not null)
            return Result<ProductDestructionDto>.Failure(pictureValidation);

        try
        {
            var trimmedDocNo = docNo.Trim();
            var document = await repository.UpdateAsync(trimmedDocNo, command, ct);
            if (document is null)
                return Result<ProductDestructionDto>.Failure($"Product destruction document not found: {docNo}");

            var hydrated = await HydrateItemNamesAsync(document, ct);
            await erpProductionRepository.SaveProductDestructionDocumentAsync(hydrated, ct);
            await erpStockRequestProcessor.ProcessStockRequestAsync(GetStockProcessItemCodes(hydrated), ct);
            return Result<ProductDestructionDto>.Success(hydrated);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<ProductDestructionDto>.Failure($"Failed to save product destruction to ERP: {ex.Message}");
        }
    }

    private static string? Validate(
        string docNo,
        string whCode,
        string shelfCode,
        IReadOnlyList<CreateProductDestructionDetailCommand> details)
    {
        if (string.IsNullOrWhiteSpace(docNo))
            return "Document number is required.";

        if (string.IsNullOrWhiteSpace(whCode))
            return "Warehouse is required.";

        if (string.IsNullOrWhiteSpace(shelfCode))
            return "Location is required.";

        if (details.Count == 0)
            return "At least one item line is required.";

        if (details.Any(d => string.IsNullOrWhiteSpace(d.ItemCode)))
            return "Every item line must have an item code.";

        if (details.Any(d => d.Qty <= 0))
            return "Every item line quantity must be greater than zero.";

        return null;
    }

    private static string? ValidatePictures(
        IReadOnlyList<CreateProductDestructionPictureCommand> pictures)
    {
        if (pictures.Any(p => p.ImageFile.Length == 0))
            return "Every picture must have image data.";

        if (pictures.Any(p => !Guid.TryParse(p.ImageGuid, out _)))
            return "Every picture must have a valid image GUID.";

        return null;
    }

    private static IReadOnlyList<string> GetStockProcessItemCodes(ProductDestructionDto document) =>
        document.Details
            .Select(d => d.ItemCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task<ProductDestructionDto> HydrateItemNamesAsync(
        ProductDestructionDto document,
        CancellationToken ct)
    {
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var details = new List<ProductDestructionDetailDto>(document.Details.Count);

        foreach (var detail in document.Details)
        {
            var itemName = await LookupItemNameAsync(detail.ItemCode, cache, ct);
            details.Add(detail with { ItemName = itemName });
        }

        return document with { Details = details };
    }

    private async Task<string> LookupItemNameAsync(
        string itemCode,
        Dictionary<string, string> cache,
        CancellationToken ct)
    {
        if (cache.TryGetValue(itemCode, out var cached))
            return cached;

        var item = await erpItemRepository.GetItemByCodeAsync(itemCode, ct);
        var name = string.IsNullOrWhiteSpace(item?.Name) ? itemCode : item.Name;
        cache[itemCode] = name;
        return name;
    }
}
