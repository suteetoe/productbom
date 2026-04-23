using BomApp.Infrastructure.Persistence.Repositories;
using BomApp.Shared.Contracts;
using FluentAssertions;

namespace BomApp.Tests.Integration.Repositories;

public class BomRepositoryIntegrationTests : BomDbIntegrationTestBase
{
    [Fact]
    public async Task CreateAsync_ShouldPersistBom_AndReturnWithId()
    {
        // Arrange
        var repo = new BomRepository(DbContext);
        var cmd = new CreateBomCommand(
            Code: "INT-BOM-001",
            Name: "Integration Test BOM",
            Description: "test",
            ItemCode: "PROD-001",
            YieldQuantity: 2m,
            YieldUnit: "PCS",
            Lines: new List<CreateBomLineCommand>
            {
                new("MAT-A", 5m, "KG", null, 1, null)
            }
        );

        // Act
        var created = await repo.CreateAsync(cmd, "test-user");

        // Assert
        created.Id.Should().NotBeEmpty();
        created.Code.Should().Be("INT-BOM-001");
        created.Status.Should().Be("Draft");
        created.Lines.Should().HaveCount(1);
        created.Lines[0].MaterialCode.Should().Be("MAT-A");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCreatedBoms()
    {
        // Arrange
        var repo = new BomRepository(DbContext);
        var cmd = new CreateBomCommand(
            Code: "INT-BOM-002",
            Name: "Integration Test BOM 2",
            Description: null,
            ItemCode: "PROD-002",
            YieldQuantity: 1m,
            YieldUnit: "KG",
            Lines: new List<CreateBomLineCommand>
            {
                new("MAT-B", 3m, "L", null, 1, null)
            }
        );
        await repo.CreateAsync(cmd, "test-user");

        // Act
        var all = await repo.GetAllAsync();

        // Assert
        all.Should().NotBeEmpty();
        all.Should().Contain(b => b.Code == "INT-BOM-002");
    }

    [Fact]
    public async Task ExistsCodeAsync_WhenCodeExists_ShouldReturnTrue()
    {
        // Arrange
        var repo = new BomRepository(DbContext);
        var cmd = new CreateBomCommand(
            Code: "INT-BOM-003",
            Name: "Exists Code Test",
            Description: null,
            ItemCode: "PROD-003",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Lines: new List<CreateBomLineCommand> { new("MAT-C", 1m, "KG", null, 1, null) }
        );
        await repo.CreateAsync(cmd, "test-user");

        // Act
        var exists = await repo.ExistsCodeAsync("INT-BOM-003");

        // Assert
        exists.Should().BeTrue();
    }
}
