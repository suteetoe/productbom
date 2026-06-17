using BomApp.Application.Interfaces.Repositories;
using BomApp.Application.Services;
using BomApp.Shared.Contracts;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Unit.Services;

public class ProductDestructionServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenDocumentIsValid_SavesHydratedDocumentToErpAndProcessesStock()
    {
        var guid = Guid.NewGuid().ToString("N");
        var created = new ProductDestructionDto(
            DocNo: "PD-20260616-00001",
            DocDate: new DateOnly(2026, 6, 16),
            WhCode: "WH01",
            ShelfCode: "A01",
            Remark: "damaged",
            Pictures:
            [
                new ProductDestructionPictureDto("PD-20260616-00001", 1, guid, [1, 2, 3])
            ],
            Details:
            [
                new ProductDestructionDetailDto("PD-20260616-00001", "FG-001", string.Empty, 2m, "PCS", "WH01", "A01", 1),
                new ProductDestructionDetailDto("PD-20260616-00001", "FG-001", string.Empty, 1m, "PCS", "WH01", "A01", 2)
            ]);

        var repository = new Mock<IProductDestructionRepository>();
        repository
            .Setup(r => r.GetByDocNoAsync(created.DocNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDestructionDto?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<CreateProductDestructionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var itemRepository = new Mock<IErpItemRepository>();
        itemRepository
            .Setup(r => r.GetItemByCodeAsync("FG-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErpItemDto("FG-001", "Finished Good", "PCS"));

        ProductDestructionDto? savedToErp = null;
        var erpProductionRepository = new Mock<IErpProductionRepository>();
        erpProductionRepository
            .Setup(r => r.SaveProductDestructionDocumentAsync(It.IsAny<ProductDestructionDto>(), It.IsAny<CancellationToken>()))
            .Callback<ProductDestructionDto, CancellationToken>((document, _) => savedToErp = document)
            .Returns(Task.CompletedTask);
        var stockRequestProcessor = new Mock<IErpStockRequestProcessor>();
        stockRequestProcessor
            .Setup(r => r.ProcessStockRequestAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ProductDestructionService(
            repository.Object,
            itemRepository.Object,
            erpProductionRepository.Object,
            stockRequestProcessor.Object);

        var result = await service.CreateAsync(new CreateProductDestructionCommand(
            created.DocNo,
            created.DocDate,
            created.WhCode,
            created.ShelfCode,
            created.Remark,
            [new CreateProductDestructionPictureCommand(guid, [1, 2, 3], 1)],
            [
                new CreateProductDestructionDetailCommand("FG-001", 2m, "PCS", "WH01", "A01", 1),
                new CreateProductDestructionDetailCommand("FG-001", 1m, "PCS", "WH01", "A01", 2)
            ]));

        result.IsSuccess.Should().BeTrue();
        savedToErp.Should().NotBeNull();
        savedToErp!.DocNo.Should().Be(created.DocNo);
        savedToErp.Details.Should().OnlyContain(d => d.ItemName == "Finished Good");
        stockRequestProcessor.Verify(
            r => r.ProcessStockRequestAsync(
                It.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "FG-001" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenPictureGuidIsInvalid_DoesNotWriteErp()
    {
        var erpProductionRepository = new Mock<IErpProductionRepository>();
        var service = new ProductDestructionService(
            Mock.Of<IProductDestructionRepository>(),
            Mock.Of<IErpItemRepository>(),
            erpProductionRepository.Object,
            Mock.Of<IErpStockRequestProcessor>());

        var result = await service.CreateAsync(new CreateProductDestructionCommand(
            "PD-20260616-00001",
            new DateOnly(2026, 6, 16),
            "WH01",
            "A01",
            "damaged",
            [new CreateProductDestructionPictureCommand("not-a-guid", [1], 1)],
            [new CreateProductDestructionDetailCommand("FG-001", 2m, "PCS", "WH01", "A01", 1)]));

        result.IsSuccess.Should().BeFalse();
        erpProductionRepository.Verify(
            r => r.SaveProductDestructionDocumentAsync(It.IsAny<ProductDestructionDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
