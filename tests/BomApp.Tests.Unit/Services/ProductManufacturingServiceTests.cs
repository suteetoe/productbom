using BomApp.Application.Interfaces.Repositories;
using BomApp.Application.Services;
using BomApp.Shared.Contracts;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Unit.Services;

public class ProductManufacturingServiceTests
{
    [Fact]
    public async Task CalculateAsync_WhenFinishedGoodHasActiveBom_ReturnsMaterialRequirements()
    {
        var bomId = Guid.NewGuid();
        var bom = CreateBom(bomId);
        var repository = new Mock<IProductManufacturingRepository>();
        var assignmentRepository = new Mock<IBomAssignmentRepository>();
        var bomRepository = new Mock<IBomRepository>();
        var erpItemRepository = new Mock<IErpItemRepository>();

        assignmentRepository
            .Setup(r => r.GetAssignedItemCodesAsync(
                It.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "FG-001" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["FG-001"] = bomId });
        bomRepository
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bom);
        erpItemRepository
            .Setup(r => r.GetItemByCodeAsync("FG-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErpItemDto("FG-001", "Finished Good", "PCS"));
        erpItemRepository
            .Setup(r => r.GetItemByCodeAsync("RM-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErpItemDto("RM-001", "Raw Material", "KG"));

        var service = new ProductManufacturingService(
            repository.Object,
            assignmentRepository.Object,
            bomRepository.Object,
            erpItemRepository.Object,
            Mock.Of<IErpProductionRepository>(),
            Mock.Of<IErpStockRequestProcessor>());

        var result = await service.CalculateAsync(new CalculateProductManufacturingRequest(
            new DateOnly(2026, 6, 17),
            "MP-20260617-00001",
            "WH01",
            "A01",
            "make to stock",
            [new CreateProductManufacturingFinishGoodCommand("FG-001", 3m, "PCS", "WH-FG", "S-FG", 0m, 0m, 1)],
            DryRun: true));

        result.IsSuccess.Should().BeTrue();
        result.Value!.FinishGoods.Should().ContainSingle(f => f.ItemCode == "FG-001" && f.ItemName == "Finished Good");
        result.Value.Materials.Should().ContainSingle(m =>
            m.ItemCode == "RM-001" &&
            m.ItemName == "Raw Material" &&
            m.Qty == 6m &&
            m.UnitCode == "KG" &&
            m.WhCode == "WH01" &&
            m.ShelfCode == "A01");
    }

    [Fact]
    public async Task CreateAsync_WhenDocumentIsValid_SavesDocumentToRepository()
    {
        CreateProductManufacturingCommand? captured = null;
        var saved = new ProductManufacturingDto(
            "MP-20260617-00001",
            new DateOnly(2026, 6, 17),
            "WH01",
            "A01",
            "make to stock",
            36m,
            [new ProductManufacturingFinishGoodDto("MP-20260617-00001", "FG-001", string.Empty, 3m, "PCS", "WH-FG", "S-FG", 12m, 36m, 1)],
            [new ProductManufacturingMaterialDto("MP-20260617-00001", "RM-001", string.Empty, 6m, "KG", "WH01", "A01", 6m, 36m, 1)]);

        var repository = new Mock<IProductManufacturingRepository>();
        repository
            .Setup(r => r.GetByDocNoAsync(saved.DocNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductManufacturingDto?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<CreateProductManufacturingCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CreateProductManufacturingCommand, CancellationToken>((command, _) => captured = command)
            .ReturnsAsync(saved);

        var erpItemRepository = new Mock<IErpItemRepository>();
        erpItemRepository
            .Setup(r => r.GetItemByCodeAsync("FG-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErpItemDto("FG-001", "Finished Good", "PCS"));
        erpItemRepository
            .Setup(r => r.GetItemByCodeAsync("RM-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErpItemDto("RM-001", "Raw Material", "KG"));
        var erpProductionRepository = new Mock<IErpProductionRepository>();
        var stockRequestProcessor = new Mock<IErpStockRequestProcessor>();

        var service = new ProductManufacturingService(
            repository.Object,
            Mock.Of<IBomAssignmentRepository>(),
            Mock.Of<IBomRepository>(),
            erpItemRepository.Object,
            erpProductionRepository.Object,
            stockRequestProcessor.Object);

        var result = await service.CreateAsync(new CreateProductManufacturingCommand(
            saved.DocNo,
            saved.DocDate,
            saved.WhCode,
            saved.ShelfCode,
            saved.Remark,
            [new CreateProductManufacturingFinishGoodCommand("FG-001", 3m, "PCS", "WH-FG", "S-FG", 12m, 36m, 1)],
            [new CreateProductManufacturingMaterialCommand("RM-001", "Raw Material", 6m, "KG", "WH01", "A01", 6m, 36m, 1)]));

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.FinishGoods.Should().ContainSingle(f => f.ItemCode == "FG-001");
        captured.Materials.Should().ContainSingle(m => m.ItemCode == "RM-001" && m.Qty == 6m && m.CostPerUnit == 6m && m.TotalCost == 36m);
        result.Value!.Materials.Should().OnlyContain(m => !string.IsNullOrWhiteSpace(m.ItemName));
        erpProductionRepository.Verify(
            r => r.SaveProductManufacturingDocumentAsync(
                It.Is<ProductManufacturingDto>(document =>
                    document.DocNo == saved.DocNo &&
                    document.TotalCost == 36m &&
                    document.FinishGoods.Single().ItemName == "Finished Good" &&
                    document.FinishGoods.Single().CostPerUnit == 12m &&
                    document.FinishGoods.Single().TotalCost == 36m &&
                    document.Materials.Single().ItemName == "Raw Material"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        stockRequestProcessor.Verify(
            r => r.ProcessStockRequestAsync(
                It.Is<IReadOnlyList<string>>(codes =>
                    codes.Contains("FG-001") &&
                    codes.Contains("RM-001")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenDocumentIsDeleted_DeletesErpManufacturingDocument()
    {
        const string docNo = "MP-20260617-00001";
        var repository = new Mock<IProductManufacturingRepository>();
        repository
            .Setup(r => r.DeleteAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var erpProductionRepository = new Mock<IErpProductionRepository>();

        var service = new ProductManufacturingService(
            repository.Object,
            Mock.Of<IBomAssignmentRepository>(),
            Mock.Of<IBomRepository>(),
            Mock.Of<IErpItemRepository>(),
            erpProductionRepository.Object,
            Mock.Of<IErpStockRequestProcessor>());

        var result = await service.DeleteAsync(docNo);

        result.IsSuccess.Should().BeTrue();
        erpProductionRepository.Verify(
            r => r.DeleteProductManufacturingDocumentAsync(docNo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenDocumentIsMissing_DoesNotDeleteErpDocument()
    {
        const string docNo = "MP-20260617-00001";
        var repository = new Mock<IProductManufacturingRepository>();
        repository
            .Setup(r => r.DeleteAsync(docNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var erpProductionRepository = new Mock<IErpProductionRepository>();

        var service = new ProductManufacturingService(
            repository.Object,
            Mock.Of<IBomAssignmentRepository>(),
            Mock.Of<IBomRepository>(),
            Mock.Of<IErpItemRepository>(),
            erpProductionRepository.Object,
            Mock.Of<IErpStockRequestProcessor>());

        var result = await service.DeleteAsync(docNo);

        result.IsSuccess.Should().BeFalse();
        erpProductionRepository.Verify(
            r => r.DeleteProductManufacturingDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static BomDto CreateBom(Guid bomId) =>
        new(
            bomId,
            "BOM-FG-001",
            "Finished Good BOM",
            null,
            "FG-001",
            "Finished Good",
            1m,
            "PCS",
            1,
            "Active",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "test",
            [new BomLineDto(Guid.NewGuid(), "RM-001", string.Empty, 2m, "KG", null, 1, null)]);
}
