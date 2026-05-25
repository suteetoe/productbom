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
            Details:
            [
                new("PROD-001", 10m, "PCS"),
                new("PROD-002", 2.5m, "BOX")
            ]);

        // Act
        var created = await repo.CreateAsync(cmd);

        // Assert
        created.Id.Should().NotBeEmpty();
        created.DocDate.Should().Be(cmd.DocDate);
        created.DocNo.Should().StartWith("BP-20260523-");
        created.DocTime.Should().Be(cmd.DocTime);
        created.Details.Should().HaveCount(2);
        created.Details.Should().Contain(d =>
            d.DocNo == created.DocNo &&
            d.ItemCode == "PROD-001" &&
            d.Qty == 10m &&
            d.UnitCode == "PCS");

        DbContext.BomProductions.Should().ContainSingle(p => p.DocNo == created.DocNo);
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
            Details: [new("PROD-001", 10m, "PCS")]));

        await repo.CreateAsync(new CreateBomProductionInternalCommand(
            DocDate: new DateOnly(2026, 5, 24),
            DocTime: new TimeOnly(9, 15, 30),
            Details: [new("PROD-002", 5m, "PCS")]));

        // Act
        var documents = await repo.GetAllAsync(itemCode: "PROD-001");
        var details = await repo.GetDetailsByDocNoAsync(matching.DocNo);

        // Assert
        documents.Should().ContainSingle().Which.DocNo.Should().Be(matching.DocNo);
        details.Should().ContainSingle(d =>
            d.DocNo == matching.DocNo &&
            d.ItemCode == "PROD-001" &&
            d.Qty == 10m &&
            d.UnitCode == "PCS");
    }

    [Fact]
    public async Task DeleteByDocNoAsync_ShouldDeleteHeaderAndDetails()
    {
        // Arrange
        var repo = new BomProductionRepository(DbContext);
        var created = await repo.CreateAsync(new CreateBomProductionInternalCommand(
            DocDate: new DateOnly(2026, 5, 23),
            DocTime: new TimeOnly(9, 15, 30),
            Details:
            [
                new("PROD-001", 10m, "PCS"),
                new("PROD-002", 5m, "PCS")
            ]));

        // Act
        var deleted = await repo.DeleteByDocNoAsync(created.DocNo);

        // Assert
        deleted.Should().BeTrue();
        DbContext.BomProductions.Should().NotContain(p => p.DocNo == created.DocNo);
        DbContext.BomProductionDetails.Should().NotContain(d => d.DocNo == created.DocNo);
    }
}
