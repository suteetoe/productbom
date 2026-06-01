using BomApp.Application.Interfaces;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;
using BomApp.UI.Services;
using BomApp.UI.ViewModels.Bom;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Unit.ViewModels;

public class BomListViewModelTests
{
    [Fact]
    public async Task LoadCommand_WhenScreenOpens_LoadsFirstBomPage()
    {
        var bom = BuildBom("BOM-001");
        var service = new Mock<IBomService>();
        service
            .Setup(s => s.GetPageAsync(
                It.Is<BomListQuery>(q => q.PageNumber == 1 && q.PageSize == 20 && q.SearchText == string.Empty),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<BomDto>>.Success(new PagedResult<BomDto>([bom], 25, 1, 20)));

        var vm = new BomListViewModel(service.Object, Mock.Of<INavigationService>(), Mock.Of<IDialogService>());

        await vm.LoadCommand.ExecuteAsync(null);

        vm.Items.Should().ContainSingle().Which.Should().Be(bom);
        vm.TotalCount.Should().Be(25);
        vm.TotalPages.Should().Be(2);
        vm.CanGoNext.Should().BeTrue();
        vm.CanGoPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task NextPageCommand_WhenMorePages_LoadsNextBomPage()
    {
        var firstPageBom = BuildBom("BOM-001");
        var secondPageBom = BuildBom("BOM-021");
        var service = new Mock<IBomService>();
        service
            .Setup(s => s.GetPageAsync(
                It.Is<BomListQuery>(q => q.PageNumber == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<BomDto>>.Success(new PagedResult<BomDto>([firstPageBom], 25, 1, 20)));
        service
            .Setup(s => s.GetPageAsync(
                It.Is<BomListQuery>(q => q.PageNumber == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<BomDto>>.Success(new PagedResult<BomDto>([secondPageBom], 25, 2, 20)));

        var vm = new BomListViewModel(service.Object, Mock.Of<INavigationService>(), Mock.Of<IDialogService>());

        await vm.LoadCommand.ExecuteAsync(null);
        await vm.NextPageCommand.ExecuteAsync(null);

        vm.PageNumber.Should().Be(2);
        vm.Items.Should().ContainSingle().Which.Should().Be(secondPageBom);
        vm.CanGoNext.Should().BeFalse();
        vm.CanGoPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task SearchText_WhenChanged_ReloadsFirstPageWithSearchText()
    {
        var bom = BuildBom("BOM-FILTERED");
        var service = new Mock<IBomService>();
        service
            .Setup(s => s.GetPageAsync(
                It.Is<BomListQuery>(q => q.SearchText == "filtered" && q.PageNumber == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<BomDto>>.Success(new PagedResult<BomDto>([bom], 1, 1, 20)));

        var vm = new BomListViewModel(service.Object, Mock.Of<INavigationService>(), Mock.Of<IDialogService>());

        vm.SearchText = "filtered";
        await Task.Delay(50);

        vm.PageNumber.Should().Be(1);
        vm.Items.Should().ContainSingle().Which.Should().Be(bom);
    }

    private static BomDto BuildBom(string code) => new(
        Id: Guid.NewGuid(),
        Code: code,
        Name: $"Formula {code}",
        Description: null,
        ItemCode: "PROD-001",
        ItemName: "Product 001",
        YieldQuantity: 1m,
        YieldUnit: "PCS",
        Version: 1,
        Status: "Draft",
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow,
        CreatedBy: "SYSTEM",
        Lines: []);
}
