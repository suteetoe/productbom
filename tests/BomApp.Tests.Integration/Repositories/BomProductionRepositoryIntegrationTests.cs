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
}
