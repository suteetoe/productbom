using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;

namespace BomApp.Tests.Fakes;

public class FakeErpItemRepository : IErpItemRepository
{
    private readonly List<ErpItemDto> _items = new()
    {
        SeedData.ItemWithBom1,
        SeedData.ItemWithBom2,
        SeedData.MaterialA,
        SeedData.ItemWithoutBom
    };

    private readonly List<ErpUnitDto> _units = new()
    {
        SeedData.Unit_PROD001_PCS,
        SeedData.Unit_PROD001_BOX,
        SeedData.Unit_PROD001_CTN,
        SeedData.Unit_PROD002_KG
    };

    public Task<IReadOnlyList<ErpItemDto>> GetAllItemsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ErpItemDto>>(_items.AsReadOnly());

    public Task<PagedResult<ErpItemDto>> GetItemsPageAsync(ErpItemListQuery query, CancellationToken ct = default)
    {
        var filtered = string.IsNullOrWhiteSpace(query.SearchText)
            ? _items
            : _items
                .Where(i => i.Code.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase)
                         || i.Name.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Max(1, query.PageSize);
        var pageItems = filtered
            .OrderBy(i => i.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PagedResult<ErpItemDto>(pageItems, filtered.Count, pageNumber, pageSize));
    }

    public Task<IReadOnlyList<ErpItemDto>> SearchItemsAsync(string keyword, CancellationToken ct = default)
    {
        var result = _items
            .Where(i => i.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                     || i.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<ErpItemDto>>(result);
    }

    public Task<ErpItemDto?> GetItemByCodeAsync(string code, CancellationToken ct = default)
        => Task.FromResult<ErpItemDto?>(_items.FirstOrDefault(i => i.Code == code));

    public Task<IReadOnlyList<ErpUnitDto>> GetUnitsByItemCodeAsync(string icCode, CancellationToken ct = default)
    {
        var result = _units.Where(u => u.IcCode == icCode).ToList();
        return Task.FromResult<IReadOnlyList<ErpUnitDto>>(result);
    }

    public Task<IReadOnlyList<ErpUnitDto>> GetAllUnitsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ErpUnitDto>>(_units.AsReadOnly());
}
