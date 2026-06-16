using BomApp.Domain.Entities;
using BomApp.Shared.Contracts;
using BomApp.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Persistence.Repositories;

public class ProductDestructionRepository(BomDbContext context) : IProductDestructionRepository
{
    public async Task<PagedResult<ProductDestructionDto>> GetPageAsync(
        ProductDestructionListQuery query,
        CancellationToken ct = default)
    {
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Max(1, query.PageSize);

        var documentsQuery = ApplyFilters(
            context.ProductDestructions.AsNoTracking().AsQueryable(),
            query.DocDateFrom,
            query.DocDateTo,
            query.DocNo);

        var totalCount = await documentsQuery.CountAsync(ct);
        var documents = await documentsQuery
            .Include(d => d.Pictures)
            .Include(d => d.Details)
            .OrderByDescending(d => d.DocDate)
            .ThenByDescending(d => d.DocNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ProductDestructionDto>(
            documents.Select(MapToDto).ToList(),
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<ProductDestructionDto?> GetByDocNoAsync(
        string docNo,
        CancellationToken ct = default)
    {
        var document = await context.ProductDestructions
            .AsNoTracking()
            .Include(d => d.Pictures)
            .Include(d => d.Details)
            .FirstOrDefaultAsync(d => d.DocNo == docNo, ct);

        return document is null ? null : MapToDto(document);
    }

    public async Task<ProductDestructionDto> CreateAsync(
        CreateProductDestructionCommand command,
        CancellationToken ct = default)
    {
        var document = new ProductDestruction
        {
            DocNo = command.DocNo.Trim(),
            DocDate = command.DocDate,
            WhCode = command.WhCode.Trim(),
            ShelfCode = command.ShelfCode.Trim(),
            Remark = command.Remark.Trim(),
            Pictures = command.Pictures
                .OrderBy(p => p.LineNumber)
                .Select(p => new ProductDestructionPicture
                {
                    DocNo = command.DocNo.Trim(),
                    LineNumber = p.LineNumber,
                    ImageGuid = p.ImageGuid,
                    ImageFile = p.ImageFile
                })
                .ToList(),
            Details = command.Details
                .OrderBy(d => d.LineNumber)
                .Select(d => new ProductDestructionDetail
                {
                    DocNo = command.DocNo.Trim(),
                    ItemCode = d.ItemCode.Trim(),
                    Qty = d.Qty,
                    UnitCode = d.UnitCode.Trim(),
                    WhCode = d.WhCode.Trim(),
                    ShelfCode = d.ShelfCode.Trim(),
                    LineNumber = d.LineNumber
                })
                .ToList()
        };

        context.ProductDestructions.Add(document);
        await context.SaveChangesAsync(ct);
        return MapToDto(document);
    }

    public async Task<ProductDestructionDto?> UpdateAsync(
        string docNo,
        UpdateProductDestructionCommand command,
        CancellationToken ct = default)
    {
        var document = await context.ProductDestructions
            .Include(d => d.Pictures)
            .Include(d => d.Details)
            .FirstOrDefaultAsync(d => d.DocNo == docNo, ct);

        if (document is null)
            return null;

        document.DocDate = command.DocDate;
        document.WhCode = command.WhCode.Trim();
        document.ShelfCode = command.ShelfCode.Trim();
        document.Remark = command.Remark.Trim();

        context.ProductDestructionPictures.RemoveRange(document.Pictures);
        context.ProductDestructionDetails.RemoveRange(document.Details);

        document.Pictures = command.Pictures
            .OrderBy(p => p.LineNumber)
            .Select(p => new ProductDestructionPicture
            {
                DocNo = docNo,
                LineNumber = p.LineNumber,
                ImageGuid = p.ImageGuid,
                ImageFile = p.ImageFile
            })
            .ToList();

        document.Details = command.Details
            .OrderBy(d => d.LineNumber)
            .Select(d => new ProductDestructionDetail
            {
                DocNo = docNo,
                ItemCode = d.ItemCode.Trim(),
                Qty = d.Qty,
                UnitCode = d.UnitCode.Trim(),
                WhCode = d.WhCode.Trim(),
                ShelfCode = d.ShelfCode.Trim(),
                LineNumber = d.LineNumber
            })
            .ToList();

        await context.SaveChangesAsync(ct);
        return MapToDto(document);
    }

    private static IQueryable<ProductDestruction> ApplyFilters(
        IQueryable<ProductDestruction> query,
        DateOnly? docDateFrom,
        DateOnly? docDateTo,
        string? docNo)
    {
        if (docDateFrom.HasValue)
            query = query.Where(d => d.DocDate >= docDateFrom.Value);

        if (docDateTo.HasValue)
            query = query.Where(d => d.DocDate <= docDateTo.Value);

        if (!string.IsNullOrWhiteSpace(docNo))
            query = query.Where(d => d.DocNo.Contains(docNo));

        return query;
    }

    private static ProductDestructionDto MapToDto(ProductDestruction document) =>
        new(
            document.DocNo,
            document.DocDate,
            document.WhCode,
            document.ShelfCode,
            document.Remark,
            document.Pictures
                .OrderBy(p => p.LineNumber)
                .Select(p => new ProductDestructionPictureDto(
                    p.DocNo,
                    p.LineNumber,
                    p.ImageGuid,
                    p.ImageFile))
                .ToList(),
            document.Details
                .OrderBy(d => d.LineNumber)
                .Select(d => new ProductDestructionDetailDto(
                    d.DocNo,
                    d.ItemCode,
                    string.Empty,
                    d.Qty,
                    d.UnitCode,
                    d.WhCode,
                    d.ShelfCode,
                    d.LineNumber))
                .ToList());
}
