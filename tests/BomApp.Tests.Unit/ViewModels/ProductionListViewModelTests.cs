using BomApp.Application.Interfaces;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;
using BomApp.UI.ViewModels.Production;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Unit.ViewModels;

public class ProductionListViewModelTests
{
    [Fact]
    public async Task LoadInitialCommand_WhenScreenOpens_LoadsProductionOrders()
    {
        // Arrange
        var order = new ProductionOrderDto(
            Id: Guid.NewGuid(),
            OrderNo: "PO-202605-00001",
            BomId: Guid.NewGuid(),
            BomCode: "BOM-001",
            ItemCode: "FG-001",
            ItemName: "Finished good",
            Quantity: 12m,
            Status: "Pending",
            SourceSoNumbers: ["SO-001"],
            SourceDocDateFrom: new DateOnly(2026, 5, 23),
            SourceDocDateTo: new DateOnly(2026, 5, 23),
            CreatedBy: "SYSTEM",
            CreatedVia: "CLI",
            CreatedAt: new DateTime(2026, 5, 23, 8, 0, 0),
            Notes: null);

        var service = new Mock<IProductionService>();
        service
            .Setup(s => s.GetOrdersAsync(
                null,
                null,
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ProductionOrderDto>>.Success([order]));

        var vm = new ProductionListViewModel(service.Object);

        // Act
        await vm.LoadInitialCommand.ExecuteAsync(null);

        // Assert
        vm.Orders.Should().ContainSingle().Which.Should().Be(order);
        vm.HasOrders.Should().BeTrue();
        service.Verify(s => s.GetOrdersAsync(
            null,
            null,
            null,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
