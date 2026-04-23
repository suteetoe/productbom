using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Application.Services;
using BomApp.Shared.Contracts;
using Moq;
using FluentAssertions;

namespace BomApp.Tests.Unit.Services;

public class BomServiceTests
{
    private readonly Mock<IBomRepository> _bomRepoMock = new();
    private readonly Mock<IBomAssignmentRepository> _assignmentRepoMock = new();

    [Fact]
    public async Task CreateAsync_WhenCodeAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        _bomRepoMock
            .Setup(r => r.ExistsCodeAsync("BOM-DUPLICATE", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = BuildService();

        var cmd = new CreateBomCommand(
            Code: "BOM-DUPLICATE",
            Name: "สูตรซ้ำ",
            Description: null,
            ItemCode: "PROD-001",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Lines: new List<CreateBomLineCommand>
            {
                new("MAT-A", 1m, "KG", null, 1, null)
            }
        );

        // Act
        var result = await service.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("ซ้ำ",
            "error message must indicate duplicate code in Thai");
    }

    [Fact]
    public async Task DeleteAsync_WhenBomIsActive_ShouldReturnFailure()
    {
        // Arrange
        var bomId = Guid.NewGuid();
        var activeBom = new BomDto(
            Id: bomId,
            Code: "BOM-001",
            Name: "สูตร Active",
            Description: null,
            ItemCode: "PROD-001",
            ItemName: "สินค้า A",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Version: 1,
            Status: "Active",           // <-- Active: delete must be rejected
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            CreatedBy: "seed",
            Lines: new List<BomLineDto>()
        );

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeBom);

        var service = BuildService();

        // Act
        var result = await service.DeleteAsync(bomId);

        // Assert
        result.IsSuccess.Should().BeFalse(
            "Active BOMs must be deactivated before deletion");
    }

    [Fact]
    public async Task CreateAsync_WithCircularReference_ShouldReturnFailure()
    {
        // Arrange
        // A BOM line references its own parent BOM as a sub-assembly → circular reference
        var parentBomId = Guid.NewGuid();

        _bomRepoMock
            .Setup(r => r.ExistsCodeAsync("BOM-CIRCULAR", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Simulate that the sub_bom_id points back to the BOM being created.
        // The application layer must detect this via DFS before persisting.
        var cmd = new CreateBomCommand(
            Code: "BOM-CIRCULAR",
            Name: "สูตร circular",
            Description: null,
            ItemCode: "PROD-001",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Lines: new List<CreateBomLineCommand>
            {
                // SubBomId intentionally set to a BOM that would create a cycle.
                // Team A's DFS validator must catch this and reject.
                new("MAT-A", 1m, "KG", parentBomId, 1, null)
            }
        );

        // Stub: the repo returns the "parent" BOM so the cycle detector can traverse it
        _bomRepoMock
            .Setup(r => r.GetByIdAsync(parentBomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BomDto(
                Id: parentBomId,
                Code: "BOM-CIRCULAR",      // same code — direct self-reference
                Name: "สูตร circular",
                Description: null,
                ItemCode: "PROD-001",
                ItemName: "สินค้า A",
                YieldQuantity: 1m,
                YieldUnit: "PCS",
                Version: 1,
                Status: "Active",
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow,
                CreatedBy: "seed",
                Lines: new List<BomLineDto>()
            ));

        var service = BuildService();

        // Act
        var result = await service.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().ContainAny(
            new[] { "circular", "วงกลม", "วนซ้ำ" },
            "error must indicate a circular reference was detected");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private IBomService BuildService()
    {
        return new BomService(_bomRepoMock.Object);
    }
}
