using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Application.UseCases;
using BomApp.Shared.Contracts;
using BomApp.Tests.Fakes;
using Moq;
using FluentAssertions;

namespace BomApp.Tests.Unit.UseCases;

public class CalculateSalesProductionUseCaseTests
{
    // Shared fakes — use real in-memory implementations, not mocks
    private readonly FakeErpItemRepository _fakeItemRepo = new();
    private readonly FakeErpSalesOrderRepository _fakeSalesRepo = new();

    // Mocked BOM-domain repositories (implemented by team-a)
    private readonly Mock<IBomRepository> _bomRepoMock = new();
    private readonly Mock<IBomAssignmentRepository> _assignmentRepoMock = new();
    private readonly Mock<IBomProductionRepository> _bomProductionRepoMock = new();
    private readonly Mock<IErpProductionRepository> _erpProductionRepoMock = new();
    private readonly Mock<IErpStockRequestProcessor> _erpStockRequestProcessorMock = new();

    public CalculateSalesProductionUseCaseTests()
    {
        _erpProductionRepoMock
            .Setup(r => r.SaveProductionDocumentAsync(It.IsAny<BomProductionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _erpStockRequestProcessorMock
            .Setup(r => r.ProcessStockRequestAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CalculateAsync_WhenItemsHaveActiveBom_ShouldReturnProductionResult()
    {
        // Arrange
        // BOM for PROD-001: yield=1, 1 material line (MAT-A, qty=2)
        var bomId = Guid.NewGuid();
        var bom = new BomDto(
            Id: bomId,
            Code: "BOM-001",
            Name: "สูตร สินค้า A",
            Description: null,
            ItemCode: "PROD-001",
            ItemName: "สินค้า A (มี BOM)",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Version: 1,
            Status: "Active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            CreatedBy: "seed",
            Lines: new List<BomLineDto>
            {
                new(Guid.NewGuid(), "MAT-A", "วัตถุดิบ A", 2m, "KG", null, 1, null)
            }
        );

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["PROD-001"] = bomId });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bom);

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 15),
            Mode: SaveMode.Daily,
            DryRun: true,
            CreatedBy: "test-user",
            CreatedVia: "UI"
        );

        var useCase = BuildUseCase();

        // Act
        var result = await useCase.CalculateAsync(request);

        // Assert
        // Sales_Day1_Doc1_PROD001: 10 PCS × stand=1/divide=1 = 10 base units
        // RequiredMaterial = (10 / yield=1) × bomLine.qty=2 = 20 KG
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.Items.Should().ContainSingle(i => i.ItemCode == "PROD-001");
        result.Value.Items.First(i => i.ItemCode == "PROD-001")
              .Materials.Should().ContainSingle(m => m.MaterialCode == "MAT-A");
    }

    [Fact]
    public async Task CalculateAsync_WhenItemHasNoBom_ShouldSkipAndWarn()
    {
        // Arrange
        // Only PROD-999 in date range (no BOM assigned)
        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid>()); // empty — no assignments

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 15),
            Mode: SaveMode.Daily,
            DryRun: true,
            CreatedBy: "test-user",
            CreatedVia: "UI"
        );

        var useCase = BuildUseCase();

        // Act
        var result = await useCase.CalculateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.SkippedItemCount.Should().BeGreaterThan(0);
        result.Value.SkippedItemCodes.Should().Contain("PROD-999");
    }

    [Fact]
    public async Task CalculateAsync_WhenUnitIsBox_ShouldConvertToBaseUnit()
    {
        // Arrange
        // SO-2024-0002: 5 BOX of PROD-001, stand_value=12, divide_value=1
        // QtyInBaseUnit = 5 × 12 / 1 = 60 PCS
        var bomId = Guid.NewGuid();
        var bom = new BomDto(
            Id: bomId,
            Code: "BOM-001",
            Name: "สูตร สินค้า A",
            Description: null,
            ItemCode: "PROD-001",
            ItemName: "สินค้า A (มี BOM)",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Version: 1,
            Status: "Active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            CreatedBy: "seed",
            Lines: new List<BomLineDto>
            {
                new(Guid.NewGuid(), "MAT-A", "วัตถุดิบ A", 2m, "KG", null, 1, null)
            }
        );

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["PROD-001"] = bomId });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bom);

        // Request only for Doc2 date range (both docs are on same day; use case must aggregate)
        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 15),
            Mode: SaveMode.Daily,
            DryRun: true,
            CreatedBy: "test-user",
            CreatedVia: "UI"
        );

        var useCase = BuildUseCase();

        // Act
        var result = await useCase.CalculateAsync(request);

        // Assert
        // Total base units for PROD-001 on Day1 = 10 (PCS doc) + 60 (BOX doc converted) = 70
        // RequiredMaterial MAT-A = (70 / yield=1) × 2 = 140 KG
        result.IsSuccess.Should().BeTrue();
        var prod001 = result.Value.Items.First(i => i.ItemCode == "PROD-001");
        prod001.QtyInBaseUnit.Should().Be(70m);
        result.Value!.Materials.Single(m => m.MaterialCode == "MAT-A").RequiredQty.Should().Be(140m);
    }

    [Fact]
    public async Task CalculateAsync_WhenBomLineMaterialNameIsBlank_ShouldUseErpItemName()
    {
        var bomId = Guid.NewGuid();
        var bom = new BomDto(
            Id: bomId,
            Code: "BOM-001",
            Name: "สูตร สินค้า A",
            Description: null,
            ItemCode: "PROD-001",
            ItemName: "สินค้า A (มี BOM)",
            YieldQuantity: 1m,
            YieldUnit: "PCS",
            Version: 1,
            Status: "Active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            CreatedBy: "seed",
            Lines:
            [
                new(Guid.NewGuid(), "MAT-A", string.Empty, 2m, "KG", null, 1, null)
            ]);

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["PROD-001"] = bomId });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bom);

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 15),
            Mode: SaveMode.Daily,
            DryRun: true,
            CreatedBy: "test-user",
            CreatedVia: "UI");

        var result = await BuildUseCase().CalculateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Materials.Single(m => m.MaterialCode == "MAT-A").MaterialName.Should().Be(SeedData.MaterialA.Name);
    }

    [Fact]
    public async Task CalculateAsync_WhenModeIsDaily_ShouldGroupByDocDate()
    {
        // Arrange
        // Day1 (2024-01-15): SO-2024-0001 + SO-2024-0002 + SO-2024-0001 (PROD-999)
        // Day2 (2024-01-16): SO-2024-0003
        // Expected: 2 bom_production documents (1 per day) when SaveMode.Daily

        var bomId1 = Guid.NewGuid();
        var bomId2 = Guid.NewGuid();

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid>
            {
                ["PROD-001"] = bomId1,
                ["PROD-002"] = bomId2
            });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMinimalBom(bomId1, "PROD-001"));

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMinimalBom(bomId2, "PROD-002"));

        _bomProductionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<CreateBomProductionInternalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBomProductionInternalCommand cmd, CancellationToken _) => new BomProductionDto(
                Id: Guid.NewGuid(),
                DocDate: cmd.DocDate,
                DocNo: $"BP-{cmd.DocDate:yyyyMMdd}-00001",
                DocTime: cmd.DocTime,
                Orders: cmd.Orders.Select(o => new BomProductionOrderDto(
                    Id: Guid.NewGuid(),
                    DocNo: $"BP-{cmd.DocDate:yyyyMMdd}-00001",
                    DocDate: cmd.DocDate,
                    RefDocNo: o.RefDocNo,
                    RefDocDate: o.RefDocDate,
                    ItemCode: o.ItemCode,
                    ItemName: o.ItemName,
                    Qty: o.Qty,
                    UnitCode: o.UnitCode)).ToList(),
                Details: cmd.Details.Select(d => new BomProductionDetailDto(
                    Id: Guid.NewGuid(),
                    DocNo: $"BP-{cmd.DocDate:yyyyMMdd}-00001",
                    ItemCode: d.ItemCode,
                    ItemName: d.ItemName,
                    Qty: d.Qty,
                    UnitCode: d.UnitCode,
                    WhCode: d.WhCode,
                    ShelfCode: d.ShelfCode)).ToList()));

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 16),
            Mode: SaveMode.Daily,
            DryRun: false,
            CreatedBy: "test-user",
            CreatedVia: "UI"
        );

        var useCase = BuildUseCase();

        // Act
        var result = await useCase.SaveAsync(request);

        // Assert: 2 bom_production documents — one per day
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(o => o.DocDate).Should()
              .Contain(new DateOnly(2024, 1, 15))
              .And.Contain(new DateOnly(2024, 1, 16));
    }

    [Fact]
    public async Task CalculateAsync_WhenModeIsPerDocument_ShouldCreateOneOrderPerDocNo()
    {
        // Arrange
        // Day1 has SO-2024-0001 (PROD-001) and SO-2024-0002 (PROD-001 in BOX)
        // Expected: 2 bom_production documents — one per unique doc_no that has items with BOM

        var bomId = Guid.NewGuid();

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["PROD-001"] = bomId });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMinimalBom(bomId, "PROD-001"));

        _bomProductionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<CreateBomProductionInternalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBomProductionInternalCommand cmd, CancellationToken _) => new BomProductionDto(
                Id: Guid.NewGuid(),
                DocDate: cmd.DocDate,
                DocNo: $"BP-{cmd.DocDate:yyyyMMdd}-{Guid.NewGuid():N}"[..30],
                DocTime: cmd.DocTime,
                Orders: cmd.Orders.Select(o => new BomProductionOrderDto(
                    Id: Guid.NewGuid(),
                    DocNo: $"BP-{cmd.DocDate:yyyyMMdd}-00001",
                    DocDate: cmd.DocDate,
                    RefDocNo: o.RefDocNo,
                    RefDocDate: o.RefDocDate,
                    ItemCode: o.ItemCode,
                    ItemName: o.ItemName,
                    Qty: o.Qty,
                    UnitCode: o.UnitCode)).ToList(),
                Details: cmd.Details.Select(d => new BomProductionDetailDto(
                    Id: Guid.NewGuid(),
                    DocNo: $"BP-{cmd.DocDate:yyyyMMdd}-00001",
                    ItemCode: d.ItemCode,
                    ItemName: d.ItemName,
                    Qty: d.Qty,
                    UnitCode: d.UnitCode,
                    WhCode: d.WhCode,
                    ShelfCode: d.ShelfCode)).ToList()));

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 15),
            Mode: SaveMode.PerDocument,
            DryRun: false,
            CreatedBy: "test-user",
            CreatedVia: "UI"
        );

        var useCase = BuildUseCase();

        // Act
        var result = await useCase.SaveAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.SelectMany(o => o.Orders).Should().OnlyContain(d => d.ItemCode == "PROD-001");
        result.Value.Select(o => o.Orders.Single().Qty).Should()
            .Contain(10m)
            .And.Contain(5m);
        result.Value.SelectMany(o => o.Details).Should().OnlyContain(d => d.ItemCode == "MAT-A");
    }

    [Fact]
    public async Task CalculateAsync_WhenDryRunIsTrue_ShouldNotCallRepository()
    {
        // Arrange
        var bomId = Guid.NewGuid();

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["PROD-001"] = bomId });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMinimalBom(bomId, "PROD-001"));

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 15),
            Mode: SaveMode.Daily,
            DryRun: true,
            CreatedBy: "test-user",
            CreatedVia: "UI"
        );

        var useCase = BuildUseCase();

        // Act
        var result = await useCase.CalculateAsync(request);

        // Assert: DryRun must never persist production documents
        result.IsSuccess.Should().BeTrue();
        _bomProductionRepoMock.Verify(
            r => r.CreateAsync(It.IsAny<CreateBomProductionInternalCommand>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "DryRun=true must not write to IBomProductionRepository");
        _erpProductionRepoMock.Verify(
            r => r.SaveProductionDocumentAsync(It.IsAny<BomProductionDto>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "DryRun=true must not write to ERP");
        _erpStockRequestProcessorMock.Verify(
            r => r.ProcessStockRequestAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "DryRun=true must not request ERP stock processing");
    }

    [Fact]
    public async Task SaveAsync_WhenDocumentIsCreated_ShouldWriteDocumentToErp()
    {
        var bomId = Guid.NewGuid();
        var createdDocument = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2024, 1, 15),
            DocNo: "BP-20240115-00001",
            DocTime: new TimeOnly(9, 30, 0),
            Orders: [],
            Details:
            [
                new BomProductionDetailDto(
                    Id: Guid.NewGuid(),
                    DocNo: "BP-20240115-00001",
                    ItemCode: "MAT-A",
                    ItemName: "Material A",
                    Qty: 10m,
                    UnitCode: "KG")
            ]);

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["PROD-001"] = bomId });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMinimalBom(bomId, "PROD-001"));

        _bomProductionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<CreateBomProductionInternalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDocument);

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 15),
            Mode: SaveMode.Daily,
            DryRun: false,
            CreatedBy: "test-user",
            CreatedVia: "UI"
        );

        var result = await BuildUseCase().SaveAsync(request);

        result.IsSuccess.Should().BeTrue();
        _erpProductionRepoMock.Verify(
            r => r.SaveProductionDocumentAsync(createdDocument, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenDocumentIsWrittenToErp_ShouldRequestStockProcessingForMaterialCodes()
    {
        var bomId = Guid.NewGuid();
        var createdDocument = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2024, 1, 15),
            DocNo: "BP-20240115-00001",
            DocTime: new TimeOnly(9, 30, 0),
            Orders: [],
            Details:
            [
                new BomProductionDetailDto(Guid.NewGuid(), "BP-20240115-00001", "MAT-A", "Material A", 10m, "KG"),
                new BomProductionDetailDto(Guid.NewGuid(), "BP-20240115-00001", "MAT-A", "Material A", 5m, "KG"),
                new BomProductionDetailDto(Guid.NewGuid(), "BP-20240115-00001", "MAT-B", "Material B", 2m, "PCS")
            ]);

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["PROD-001"] = bomId });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMinimalBom(bomId, "PROD-001"));

        _bomProductionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<CreateBomProductionInternalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDocument);

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 15),
            Mode: SaveMode.Daily,
            DryRun: false,
            CreatedBy: "test-user",
            CreatedVia: "UI"
        );

        var result = await BuildUseCase().SaveAsync(request);

        result.IsSuccess.Should().BeTrue();
        _erpStockRequestProcessorMock.Verify(
            r => r.ProcessStockRequestAsync(
                It.Is<IReadOnlyList<string>>(codes =>
                    codes.Count == 2 &&
                    codes.Contains("MAT-A") &&
                    codes.Contains("MAT-B")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_WhenDailyModeHasDifferentSalesWarehouses_ShouldKeepWarehouseAndShelfOnDetails()
    {
        var bomId = Guid.NewGuid();
        CreateBomProductionInternalCommand? capturedCommand = null;

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["PROD-001"] = bomId });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(bomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMinimalBom(bomId, "PROD-001"));

        _bomProductionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<CreateBomProductionInternalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBomProductionInternalCommand cmd, CancellationToken _) =>
            {
                capturedCommand = cmd;
                return new BomProductionDto(
                    Id: Guid.NewGuid(),
                    DocDate: cmd.DocDate,
                    DocNo: "BP-20240115-00001",
                    DocTime: cmd.DocTime,
                    Orders: [],
                    Details: cmd.Details.Select(d => new BomProductionDetailDto(
                        Guid.NewGuid(),
                        "BP-20240115-00001",
                        d.ItemCode,
                        d.ItemName,
                        d.Qty,
                        d.UnitCode,
                        d.WhCode,
                        d.ShelfCode)).ToList());
            });

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 15),
            DateTo: new DateOnly(2024, 1, 15),
            Mode: SaveMode.Daily,
            DryRun: false,
            CreatedBy: "test-user",
            CreatedVia: "UI"
        );

        var result = await BuildUseCase().SaveAsync(request);

        result.IsSuccess.Should().BeTrue();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Details.Should().HaveCount(2);
        capturedCommand.Details.Should().Contain(d =>
            d.ItemCode == "MAT-A" &&
            d.Qty == 10m &&
            d.WhCode == "WH-A" &&
            d.ShelfCode == "SH-01");
        capturedCommand.Details.Should().Contain(d =>
            d.ItemCode == "MAT-A" &&
            d.Qty == 60m &&
            d.WhCode == "WH-B" &&
            d.ShelfCode == "SH-02");
    }

    [Fact]
    public async Task CalculateAsync_WhenBomLineReferencesSubBom_ShouldExplodeToChildMaterials()
    {
        var noodleBomId = Guid.NewGuid();
        var meatballBomId = Guid.NewGuid();

        var salesRepo = new Mock<IErpSalesOrderRepository>();
        salesRepo
            .Setup(r => r.GetSalesTransactionsByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ErpSalesTransactionDto(
                    new DateOnly(2024, 1, 20),
                    "SO-NOODLE-001",
                    "NOODLE",
                    "Noodle",
                    3m,
                    "DISH",
                    1m,
                    1m)
            ]);

        var itemRepo = new Mock<IErpItemRepository>();
        itemRepo
            .Setup(r => r.GetUnitsByItemCodeAsync("MEATBALL", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ErpUnitDto("G", "Gram", "MEATBALL", 1m, 1m, 1, 1),
                new ErpUnitDto("KG", "Kilogram", "MEATBALL", 1000m, 1m, 1000, 2)
            ]);

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["NOODLE"] = noodleBomId });

        var noodleBom = new BomDto(
            Id: noodleBomId,
            Code: "BOM-NOODLE",
            Name: "Noodle",
            Description: null,
            ItemCode: "NOODLE",
            ItemName: "Noodle",
            YieldQuantity: 1m,
            YieldUnit: "DISH",
            Version: 1,
            Status: "Active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            CreatedBy: "seed",
            Lines: new List<BomLineDto>
            {
                new(Guid.NewGuid(), "NOODLE-STRAND", "Noodle strand", 300m, "G", null, 1, null),
                new(Guid.NewGuid(), "MEATBALL", "Meatball", 300m, "G", meatballBomId, 2, null)
            });

        var meatballBom = new BomDto(
            Id: meatballBomId,
            Code: "BOM-MEATBALL",
            Name: "Meatball",
            Description: null,
            ItemCode: "MEATBALL",
            ItemName: "Meatball",
            YieldQuantity: 1m,
            YieldUnit: "KG",
            Version: 1,
            Status: "Active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            CreatedBy: "seed",
            Lines: new List<BomLineDto>
            {
                new(Guid.NewGuid(), "PORK", "Pork", 600m, "G", null, 1, null),
                new(Guid.NewGuid(), "FLOUR", "Flour", 200m, "G", null, 2, null)
            });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(noodleBomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(noodleBom);
        _bomRepoMock
            .Setup(r => r.GetByIdAsync(meatballBomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meatballBom);

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 20),
            DateTo: new DateOnly(2024, 1, 20),
            Mode: SaveMode.Daily,
            DryRun: true,
            CreatedBy: "test-user",
            CreatedVia: "UI");

        var result = await BuildUseCase(salesRepo.Object, itemRepo.Object).CalculateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Materials.Should().BeEquivalentTo([
            new MaterialRequirementDto("NOODLE-STRAND", "Noodle strand", 900m, "G"),
            new MaterialRequirementDto("PORK", "Pork", 540m, "G"),
            new MaterialRequirementDto("FLOUR", "Flour", 180m, "G")
        ]);
        result.Value.Materials.Should().NotContain(m => m.MaterialCode == "MEATBALL");
    }

    [Fact]
    public async Task CalculateAsync_WhenBomLineDoesNotReferenceSubBom_ShouldKeepMaterialItself()
    {
        var noodleBomId = Guid.NewGuid();

        var salesRepo = new Mock<IErpSalesOrderRepository>();
        salesRepo
            .Setup(r => r.GetSalesTransactionsByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ErpSalesTransactionDto(
                    new DateOnly(2024, 1, 20),
                    "SO-NOODLE-002",
                    "NOODLE",
                    "Noodle",
                    3m,
                    "DISH",
                    1m,
                    1m)
            ]);

        _assignmentRepoMock
            .Setup(r => r.GetAssignedItemCodesAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid> { ["NOODLE"] = noodleBomId });

        var noodleBom = new BomDto(
            Id: noodleBomId,
            Code: "BOM-NOODLE-PLAIN",
            Name: "Noodle without sub-BOM",
            Description: null,
            ItemCode: "NOODLE",
            ItemName: "Noodle",
            YieldQuantity: 1m,
            YieldUnit: "DISH",
            Version: 1,
            Status: "Active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            CreatedBy: "seed",
            Lines: new List<BomLineDto>
            {
                new(Guid.NewGuid(), "NOODLE-STRAND", "Noodle strand", 300m, "G", null, 1, null),
                new(Guid.NewGuid(), "MEATBALL", "Meatball", 300m, "G", null, 2, null)
            });

        _bomRepoMock
            .Setup(r => r.GetByIdAsync(noodleBomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(noodleBom);

        var request = new CalculateSalesProductionRequest(
            DateFrom: new DateOnly(2024, 1, 20),
            DateTo: new DateOnly(2024, 1, 20),
            Mode: SaveMode.Daily,
            DryRun: true,
            CreatedBy: "test-user",
            CreatedVia: "UI");

        var result = await BuildUseCase(salesRepo.Object).CalculateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Materials.Should().BeEquivalentTo([
            new MaterialRequirementDto("NOODLE-STRAND", "Noodle strand", 900m, "G"),
            new MaterialRequirementDto("MEATBALL", "Meatball", 900m, "G")
        ]);
        result.Value.Materials.Should().NotContain(m => m.MaterialCode == "PORK" || m.MaterialCode == "FLOUR");
    }

    private ICalculateSalesProductionUseCase BuildUseCase(
        IErpSalesOrderRepository? salesRepo = null,
        IErpItemRepository? itemRepo = null)
    {
        // Team A will implement CalculateSalesProductionUseCase.
        // Constructor is expected to accept the four repositories below.
        // This stub creates the concrete class once team-a merges their implementation.
        return new CalculateSalesProductionUseCase(
            salesRepo ?? _fakeSalesRepo,
            _bomRepoMock.Object,
            _assignmentRepoMock.Object,
            _bomProductionRepoMock.Object,
            _erpProductionRepoMock.Object,
            itemRepo ?? _fakeItemRepo,
            _erpStockRequestProcessorMock.Object
        );
    }

    private static BomDto BuildMinimalBom(Guid id, string itemCode) => new(
        Id: id,
        Code: $"BOM-{itemCode}",
        Name: $"สูตร {itemCode}",
        Description: null,
        ItemCode: itemCode,
        ItemName: itemCode,
        YieldQuantity: 1m,
        YieldUnit: "PCS",
        Version: 1,
        Status: "Active",
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow,
        CreatedBy: "seed",
        Lines: new List<BomLineDto>
        {
            new(Guid.NewGuid(), "MAT-A", "วัตถุดิบ A", 1m, "KG", null, 1, null)
        }
    );
}
