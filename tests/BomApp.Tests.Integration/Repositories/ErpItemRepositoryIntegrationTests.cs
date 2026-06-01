using BomApp.Infrastructure.Erp;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Tests.Integration.Repositories;

public class ErpItemRepositoryIntegrationTests : ErpDbIntegrationTestBase
{
    protected override async Task SeedAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_inventory (code, name_1, unit_cost) VALUES
              ('ITEM-001', 'สินค้าทดสอบ 1',  '100.00'),
              ('ITEM-002', 'สินค้าทดสอบ 2',  '200.00'),
              ('MAT-001',  'วัตถุดิบ ก',      '50.00')
            """);
    }

    [Fact]
    public async Task GetAllItemsAsync_ReturnsAllRows()
    {
        var repo = new ErpItemRepository(DbContext);

        var result = await repo.GetAllItemsAsync();

        result.Should().HaveCount(3);
        result.Select(r => r.Code).Should().BeEquivalentTo(["ITEM-001", "ITEM-002", "MAT-001"]);
    }

    [Fact]
    public async Task GetAllItemsAsync_OrdersByCode()
    {
        var repo = new ErpItemRepository(DbContext);

        var result = await repo.GetAllItemsAsync();

        result.Select(r => r.Code).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task SearchItemsAsync_ByCode_ReturnsMatch()
    {
        var repo = new ErpItemRepository(DbContext);

        var result = await repo.SearchItemsAsync("ITEM");

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.Code.Should().Contain("ITEM"));
    }

    [Fact]
    public async Task GetItemsPageAsync_ReturnsRequestedPageAndTotalCount()
    {
        var repo = new ErpItemRepository(DbContext);

        var result = await repo.GetItemsPageAsync(new(SearchText: null, PageNumber: 2, PageSize: 2));

        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items[0].Code.Should().Be("MAT-001");
    }

    [Fact]
    public async Task GetItemsPageAsync_WithSearchText_ReturnsFilteredTotalCount()
    {
        var repo = new ErpItemRepository(DbContext);

        var result = await repo.GetItemsPageAsync(new(SearchText: "ITEM", PageNumber: 1, PageSize: 20));

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(r => r.Code.Should().Contain("ITEM"));
    }

    [Fact]
    public async Task SearchItemsAsync_ByName_ReturnsMatch()
    {
        var repo = new ErpItemRepository(DbContext);

        var result = await repo.SearchItemsAsync("วัตถุดิบ");

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("MAT-001");
    }

    [Fact]
    public async Task SearchItemsAsync_NoMatch_ReturnsEmpty()
    {
        var repo = new ErpItemRepository(DbContext);

        var result = await repo.SearchItemsAsync("ไม่มีสินค้านี้");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetItemByCodeAsync_ExistingCode_ReturnsItem()
    {
        var repo = new ErpItemRepository(DbContext);

        var result = await repo.GetItemByCodeAsync("ITEM-001");

        result.Should().NotBeNull();
        result!.Code.Should().Be("ITEM-001");
        result.Name.Should().Be("สินค้าทดสอบ 1");
    }

    [Fact]
    public async Task GetItemByCodeAsync_UnknownCode_ReturnsNull()
    {
        var repo = new ErpItemRepository(DbContext);

        var result = await repo.GetItemByCodeAsync("DOES-NOT-EXIST");

        result.Should().BeNull();
    }
}
