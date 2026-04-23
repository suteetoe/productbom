using BomApp.Infrastructure.Persistence.Repositories;
using BomApp.Shared.Contracts;
using FluentAssertions;

namespace BomApp.Tests.Integration.Repositories;

public class BomAssignmentRepositoryIntegrationTests : BomDbIntegrationTestBase
{
    [Fact]
    public async Task AssignAsync_AndGetBomId_ShouldRoundTrip()
    {
        // Arrange — create a BOM first
        var bomRepo = new BomRepository(DbContext);
        var bom = await bomRepo.CreateAsync(new CreateBomCommand(
            Code: "ASSIGN-BOM-001",
            Name: "Assignment Test BOM",
            Description: null,
            ItemCode: "PROD-ASSIGN-001",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Lines: new List<CreateBomLineCommand> { new("MAT-X", 1m, "KG", null, 1, null) }
        ), "test-user");

        var assignRepo = new BomAssignmentRepository(DbContext);

        // Act
        await assignRepo.AssignAsync("ITEM-001", "สินค้าทดสอบ", bom.Id, "test-user");
        var bomId = await assignRepo.GetBomIdByItemCodeAsync("ITEM-001");

        // Assert
        bomId.Should().Be(bom.Id);
    }

    [Fact]
    public async Task RemoveAsync_ShouldClearAssignment()
    {
        // Arrange
        var bomRepo = new BomRepository(DbContext);
        var bom = await bomRepo.CreateAsync(new CreateBomCommand(
            Code: "ASSIGN-BOM-002",
            Name: "Remove Assignment Test BOM",
            Description: null,
            ItemCode: "PROD-ASSIGN-002",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Lines: new List<CreateBomLineCommand> { new("MAT-Y", 1m, "KG", null, 1, null) }
        ), "test-user");

        var assignRepo = new BomAssignmentRepository(DbContext);
        await assignRepo.AssignAsync("ITEM-002", "สินค้าลบ assignment", bom.Id, "test-user");

        // Act
        await assignRepo.RemoveAsync("ITEM-002");
        var bomId = await assignRepo.GetBomIdByItemCodeAsync("ITEM-002");

        // Assert
        bomId.Should().BeNull("assignment ถูกลบแล้ว");
    }
}
