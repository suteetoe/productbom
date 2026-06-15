using BomApp.Application.Interfaces.Repositories;
using BomApp.Application.Services;
using BomApp.Shared.Contracts;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Unit.Services;

public class ProductionServiceTests
{
    [Fact]
    public async Task DeleteDocumentAsync_WhenDocumentExists_DeletesErpDocumentBeforeLocalDocument()
    {
        const string docNo = "BP-20260523-00001";
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 5, 23),
            DocNo: docNo,
            DocTime: new TimeOnly(8, 0, 0),
            Orders: [],
            Details: []);

        var productionOrderRepository = new Mock<IProductionOrderRepository>();
        var bomProductionRepository = new Mock<IBomProductionRepository>();
        var erpProductionRepository = new Mock<IErpProductionRepository>();
        var erpItemRepository = new Mock<IErpItemRepository>();
        var sequence = new MockSequence();

        bomProductionRepository
            .Setup(r => r.GetByDocNoAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        erpProductionRepository
            .InSequence(sequence)
            .Setup(r => r.DeleteProductionDocumentAsync(docNo, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        bomProductionRepository
            .InSequence(sequence)
            .Setup(r => r.DeleteByDocNoAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ProductionService(
            productionOrderRepository.Object,
            bomProductionRepository.Object,
            erpProductionRepository.Object,
            erpItemRepository.Object);

        var result = await service.DeleteDocumentAsync(docNo);

        result.IsSuccess.Should().BeTrue();
        erpProductionRepository.Verify(
            r => r.DeleteProductionDocumentAsync(docNo, It.IsAny<CancellationToken>()),
            Times.Once);
        bomProductionRepository.Verify(
            r => r.DeleteByDocNoAsync(docNo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenDocumentDoesNotExist_DoesNotDeleteErpDocument()
    {
        const string docNo = "BP-20260523-99999";

        var productionOrderRepository = new Mock<IProductionOrderRepository>();
        var bomProductionRepository = new Mock<IBomProductionRepository>();
        var erpProductionRepository = new Mock<IErpProductionRepository>();
        var erpItemRepository = new Mock<IErpItemRepository>();

        bomProductionRepository
            .Setup(r => r.GetByDocNoAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProductionDto?)null);

        var service = new ProductionService(
            productionOrderRepository.Object,
            bomProductionRepository.Object,
            erpProductionRepository.Object,
            erpItemRepository.Object);

        var result = await service.DeleteDocumentAsync(docNo);

        result.IsSuccess.Should().BeFalse();
        erpProductionRepository.Verify(
            r => r.DeleteProductionDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        bomProductionRepository.Verify(
            r => r.DeleteByDocNoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDocumentDetailsAsync_WhenStoredNameIsCode_ReturnsErpItemName()
    {
        const string docNo = "BP-20260424-00001";
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 4, 24),
            DocNo: docNo,
            DocTime: new TimeOnly(8, 0, 0),
            Orders: [],
            Details: []);
        var detail = new BomProductionDetailDto(
            Id: Guid.NewGuid(),
            DocNo: docNo,
            ItemCode: "M-00001",
            ItemName: "M-00001",
            Qty: 2000m,
            UnitCode: "กรัม");

        var productionOrderRepository = new Mock<IProductionOrderRepository>();
        var bomProductionRepository = new Mock<IBomProductionRepository>();
        var erpProductionRepository = new Mock<IErpProductionRepository>();
        var erpItemRepository = new Mock<IErpItemRepository>();

        bomProductionRepository
            .Setup(r => r.GetByDocNoAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        bomProductionRepository
            .Setup(r => r.GetDetailsByDocNoAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync([detail]);
        erpItemRepository
            .Setup(r => r.GetItemByCodeAsync("M-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErpItemDto("M-00001", "แป้งสาลี", "0"));

        var service = new ProductionService(
            productionOrderRepository.Object,
            bomProductionRepository.Object,
            erpProductionRepository.Object,
            erpItemRepository.Object);

        var result = await service.GetDocumentDetailsAsync(docNo);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.ItemName.Should().Be("แป้งสาลี");
    }

    [Fact]
    public async Task GetDocumentOrdersAsync_WhenStoredNameIsCode_ReturnsErpItemName()
    {
        const string docNo = "BP-20260424-00002";
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 4, 24),
            DocNo: docNo,
            DocTime: new TimeOnly(8, 0, 0),
            Orders: [],
            Details: []);
        var order = new BomProductionOrderDto(
            Id: Guid.NewGuid(),
            DocNo: docNo,
            DocDate: new DateOnly(2026, 4, 24),
            RefDocNo: "SI2406-00001",
            RefDocDate: new DateOnly(2026, 4, 24),
            ItemCode: "IC-00001",
            ItemName: "IC-00001",
            Qty: 5m,
            UnitCode: "จาน");

        var productionOrderRepository = new Mock<IProductionOrderRepository>();
        var bomProductionRepository = new Mock<IBomProductionRepository>();
        var erpProductionRepository = new Mock<IErpProductionRepository>();
        var erpItemRepository = new Mock<IErpItemRepository>();

        bomProductionRepository
            .Setup(r => r.GetByDocNoAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        bomProductionRepository
            .Setup(r => r.GetOrdersByDocNoAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);
        erpItemRepository
            .Setup(r => r.GetItemByCodeAsync("IC-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErpItemDto("IC-00001", "ข้าวสวย", "0"));

        var service = new ProductionService(
            productionOrderRepository.Object,
            bomProductionRepository.Object,
            erpProductionRepository.Object,
            erpItemRepository.Object);

        var result = await service.GetDocumentOrdersAsync(docNo);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.ItemName.Should().Be("ข้าวสวย");
    }
}
