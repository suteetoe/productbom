using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;
using BomApp.UI.ViewModels.BomAssignment;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Unit.ViewModels;

public class BomAssignmentViewModelTests
{
    [Fact]
    public async Task LoadCommand_WhenScreenOpens_LoadsFirstErpItemPage()
    {
        var item = new ErpItemDto("ITEM-001", "Item 001", "0");
        var erpRepo = new Mock<IErpItemRepository>();
        erpRepo
            .Setup(r => r.GetItemsPageAsync(
                It.Is<ErpItemListQuery>(q => q.PageNumber == 1 && q.PageSize == 20 && q.SearchText == string.Empty),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ErpItemDto>([item], 25, 1, 20));
        var assignmentService = BuildAssignmentService();

        var vm = new BomAssignmentViewModel(assignmentService.Object, erpRepo.Object);

        await vm.LoadCommand.ExecuteAsync(null);

        vm.ErpItems.Should().ContainSingle().Which.ItemCode.Should().Be("ITEM-001");
        vm.TotalCount.Should().Be(25);
        vm.TotalPages.Should().Be(2);
        vm.CanGoNext.Should().BeTrue();
        vm.CanGoPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCommand_WhenItemHasAssignment_MarksItemAsAssigned()
    {
        var bomId = Guid.NewGuid();
        var item = new ErpItemDto("ITEM-001", "Item 001", "0");
        var erpRepo = new Mock<IErpItemRepository>();
        erpRepo
            .Setup(r => r.GetItemsPageAsync(
                It.IsAny<ErpItemListQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ErpItemDto>([item], 1, 1, 20));
        var assignmentService = BuildAssignmentService();
        assignmentService
            .Setup(s => s.GetAssignedItemCodesAsync(
                It.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "ITEM-001" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyDictionary<string, Guid>>.Success(
                new Dictionary<string, Guid> { ["ITEM-001"] = bomId }));

        var vm = new BomAssignmentViewModel(assignmentService.Object, erpRepo.Object);

        await vm.LoadCommand.ExecuteAsync(null);

        var row = vm.ErpItems.Should().ContainSingle().Subject;
        row.IsAssigned.Should().BeTrue();
        row.AssignedBomId.Should().Be(bomId);
    }

    [Fact]
    public async Task NextPageCommand_WhenMorePages_LoadsNextErpItemPage()
    {
        var firstPageItem = new ErpItemDto("ITEM-001", "Item 001", "0");
        var secondPageItem = new ErpItemDto("ITEM-021", "Item 021", "0");
        var erpRepo = new Mock<IErpItemRepository>();
        erpRepo
            .Setup(r => r.GetItemsPageAsync(
                It.Is<ErpItemListQuery>(q => q.PageNumber == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ErpItemDto>([firstPageItem], 25, 1, 20));
        erpRepo
            .Setup(r => r.GetItemsPageAsync(
                It.Is<ErpItemListQuery>(q => q.PageNumber == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ErpItemDto>([secondPageItem], 25, 2, 20));
        var assignmentService = BuildAssignmentService();

        var vm = new BomAssignmentViewModel(assignmentService.Object, erpRepo.Object);

        await vm.LoadCommand.ExecuteAsync(null);
        await vm.NextPageCommand.ExecuteAsync(null);

        vm.PageNumber.Should().Be(2);
        vm.ErpItems.Should().ContainSingle().Which.ItemCode.Should().Be("ITEM-021");
        vm.CanGoNext.Should().BeFalse();
        vm.CanGoPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task ItemSearchText_WhenChanged_ReloadsFirstPageWithSearchText()
    {
        var item = new ErpItemDto("FILTERED-001", "Filtered item", "0");
        var erpRepo = new Mock<IErpItemRepository>();
        erpRepo
            .Setup(r => r.GetItemsPageAsync(
                It.Is<ErpItemListQuery>(q => q.SearchText == "filtered" && q.PageNumber == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ErpItemDto>([item], 1, 1, 20));
        var assignmentService = BuildAssignmentService();

        var vm = new BomAssignmentViewModel(assignmentService.Object, erpRepo.Object);

        vm.ItemSearchText = "filtered";
        await Task.Delay(50);

        vm.PageNumber.Should().Be(1);
        vm.ErpItems.Should().ContainSingle().Which.ItemCode.Should().Be("FILTERED-001");
    }

    [Fact]
    public async Task AssignBomCommand_WhenBomLineMaterialNameIsBlank_LoadsMaterialNameFromErp()
    {
        var bomId = Guid.NewGuid();
        var assignedBom = new BomDto(
            Id: bomId,
            Code: "BOM-001",
            Name: "Formula 001",
            Description: null,
            ItemCode: "ITEM-001",
            ItemName: "Item 001",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Version: 1,
            Status: "Active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            CreatedBy: "test",
            Lines:
            [
                new BomLineDto(
                    Id: Guid.NewGuid(),
                    MaterialCode: "MAT-001",
                    MaterialName: string.Empty,
                    Quantity: 2m,
                    Unit: "KG",
                    SubBomId: null,
                    SortOrder: 1,
                    Notes: null)
            ]);
        var erpRepo = new Mock<IErpItemRepository>();
        erpRepo
            .Setup(r => r.GetItemByCodeAsync("MAT-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErpItemDto("MAT-001", "Material 001", "0"));
        var assignmentService = BuildAssignmentService();
        assignmentService
            .Setup(s => s.AssignAsync("ITEM-001", "Item 001", bomId, "current-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        assignmentService
            .Setup(s => s.GetAssignedBomAsync("ITEM-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BomDto?>.Success(assignedBom));
        var vm = new BomAssignmentViewModel(assignmentService.Object, erpRepo.Object)
        {
            SelectedItem = new ErpItemRow("ITEM-001", "Item 001", null, false, null),
            SelectedBomToAssign = assignedBom
        };

        await vm.AssignBomCommand.ExecuteAsync(null);

        vm.AssignedBom.Should().NotBeNull();
        vm.AssignedBom!.Lines.Single().MaterialName.Should().Be("Material 001");
    }

    private static Mock<IBomAssignmentService> BuildAssignmentService()
    {
        var assignmentService = new Mock<IBomAssignmentService>();
        assignmentService
            .Setup(s => s.GetActiveBomsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<BomDto>>.Success([]));
        assignmentService
            .Setup(s => s.GetAssignedItemCodesAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyDictionary<string, Guid>>.Success(
                new Dictionary<string, Guid>()));
        return assignmentService;
    }
}
