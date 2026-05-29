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
            erpProductionRepository.Object);

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

        bomProductionRepository
            .Setup(r => r.GetByDocNoAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProductionDto?)null);

        var service = new ProductionService(
            productionOrderRepository.Object,
            bomProductionRepository.Object,
            erpProductionRepository.Object);

        var result = await service.DeleteDocumentAsync(docNo);

        result.IsSuccess.Should().BeFalse();
        erpProductionRepository.Verify(
            r => r.DeleteProductionDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        bomProductionRepository.Verify(
            r => r.DeleteByDocNoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
