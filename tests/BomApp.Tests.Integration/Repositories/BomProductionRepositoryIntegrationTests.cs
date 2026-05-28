using BomApp.Infrastructure.Persistence.Repositories;
using BomApp.Shared.Contracts;
using FluentAssertions;

namespace BomApp.Tests.Integration.Repositories;

public class BomProductionRepositoryIntegrationTests : BomDbIntegrationTestBase
{
    [Fact]
    public async Task CreateAsync_ShouldPersistHeaderAndDetails()
    {
        // Arrange
        var repo = new BomProductionRepository(DbContext);
        var cmd = new CreateBomProductionInternalCommand(
            DocDate: new DateOnly(2026, 5, 23),
            DocTime: new TimeOnly(9, 15, 30),
            Orders:
            [
                new("SI260523-00001", new DateOnly(2026, 5, 23), "PROD-001", 10m, "PCS"),
                new("SI260523-00002", new DateOnly(2026, 5, 23), "PROD-002", 2.5m, "BOX")
            ],
            Details:
            [
                new("MAT-001", "Material 001", 20m, "KG", "WH-A", "SH-01"),
                new("MAT-002", "Material 002", 5m, "PCS", "WH-B", "SH-02")
            ]);

        // Act
        var created = await repo.CreateAsync(cmd);

        // Assert
        created.Id.Should().NotBeEmpty();
        created.DocDate.Should().Be(cmd.DocDate);
        created.DocNo.Should().StartWith("BP-20260523-");
        created.DocTime.Should().Be(cmd.DocTime);
        created.Orders.Should().HaveCount(2);
        created.Orders.Should().Contain(o =>
            o.DocNo == created.DocNo &&
            o.RefDocNo == "SI260523-00001" &&
            o.RefDocDate == new DateOnly(2026, 5, 23) &&
            o.ItemCode == "PROD-001" &&
            o.Qty == 10m &&
            o.UnitCode == "PCS");
        created.Details.Should().HaveCount(2);
        created.Details.Should().Contain(d =>
            d.DocNo == created.DocNo &&
            d.ItemCode == "MAT-001" &&
            d.ItemName == "Material 001" &&
            d.Qty == 20m &&
            d.UnitCode == "KG" &&
            d.WhCode == "WH-A" &&
            d.ShelfCode == "SH-01");

        DbContext.BomProductions.Should().ContainSingle(p => p.DocNo == created.DocNo);
        DbContext.BomProductionOrders.Should().HaveCount(2);
        DbContext.BomProductionDetails.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenFilteringByItemCode_ShouldReturnMatchingBomProductionDocuments()
    {
        // Arrange
        var repo = new BomProductionRepository(DbContext);
        var matching = await repo.CreateAsync(new CreateBomProductionInternalCommand(
            DocDate: new DateOnly(2026, 5, 23),
            DocTime: new TimeOnly(9, 15, 30),
            Orders: [new("SI260523-00001", new DateOnly(2026, 5, 23), "PROD-001", 10m, "PCS")],
            Details: [new("MAT-001", "Material 001", 20m, "KG")]));

        await repo.CreateAsync(new CreateBomProductionInternalCommand(
            DocDate: new DateOnly(2026, 5, 24),
            DocTime: new TimeOnly(9, 15, 30),
            Orders: [new("SI260524-00001", new DateOnly(2026, 5, 24), "PROD-002", 5m, "PCS")],
            Details: [new("MAT-002", "Material 002", 5m, "PCS")]));

        // Act
        var documents = await repo.GetAllAsync(itemCode: "PROD-001");
        var orders = await repo.GetOrdersByDocNoAsync(matching.DocNo);
        var details = await repo.GetDetailsByDocNoAsync(matching.DocNo);

        // Assert
        documents.Should().ContainSingle().Which.DocNo.Should().Be(matching.DocNo);
        orders.Should().ContainSingle(o =>
            o.DocNo == matching.DocNo &&
            o.RefDocNo == "SI260523-00001" &&
            o.ItemCode == "PROD-001" &&
            o.Qty == 10m &&
            o.UnitCode == "PCS");
        details.Should().ContainSingle(d =>
            d.DocNo == matching.DocNo &&
            d.ItemCode == "MAT-001" &&
            d.Qty == 20m &&
            d.UnitCode == "KG");
    }

    [Fact]
    public async Task DeleteByDocNoAsync_ShouldDeleteHeaderAndDetails()
    {
        // Arrange
        var repo = new BomProductionRepository(DbContext);
        var created = await repo.CreateAsync(new CreateBomProductionInternalCommand(
            DocDate: new DateOnly(2026, 5, 23),
            DocTime: new TimeOnly(9, 15, 30),
            Orders:
            [
                new("SI260523-00001", new DateOnly(2026, 5, 23), "PROD-001", 10m, "PCS"),
                new("SI260523-00002", new DateOnly(2026, 5, 23), "PROD-002", 5m, "PCS")
            ],
            Details:
            [
                new("MAT-001", "Material 001", 20m, "KG")
            ]));

        // Act
        var deleted = await repo.DeleteByDocNoAsync(created.DocNo);

        // Assert
        deleted.Should().BeTrue();
        DbContext.BomProductions.Should().NotContain(p => p.DocNo == created.DocNo);
        DbContext.BomProductionOrders.Should().NotContain(p => p.DocNo == created.DocNo);
        DbContext.BomProductionDetails.Should().NotContain(p => p.DocNo == created.DocNo);
    }
}
