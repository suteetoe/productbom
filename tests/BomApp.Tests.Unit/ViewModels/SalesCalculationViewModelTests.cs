using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;
using BomApp.UI.Services;
using BomApp.UI.ViewModels.SalesCalculation;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Unit.ViewModels;

public class SalesCalculationViewModelTests
{
    [Fact]
    public async Task LoadSalesCommand_WhenDateRangeSelected_ReplacesSalesTransactions()
    {
        var transaction = new ErpSalesTransactionDto(
            DocDate: new DateOnly(2026, 5, 1),
            DocNo: "SI2605-00001",
            ItemCode: "FG-001",
            ItemName: "Finished Good 001",
            Qty: 5m,
            UnitCode: "PCS",
            StandValue: 1m,
            DivideValue: 1m);
        var salesRepo = new Mock<IErpSalesOrderRepository>();
        salesRepo
            .Setup(r => r.GetSalesTransactionsByDateRangeAsync(
                new DateOnly(2026, 5, 1),
                new DateOnly(2026, 5, 25),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([transaction]);

        var vm = new SalesCalculationViewModel(
            Mock.Of<ICalculateSalesProductionUseCase>(),
            salesRepo.Object,
            Mock.Of<IDialogService>())
        {
            DateFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            DateTo = new DateTimeOffset(2026, 5, 25, 0, 0, 0, TimeSpan.Zero),
            HasCalculationResult = true,
            MaterialRequirements = [new MaterialRequirementDto("MAT-001", "Material 001", 3m, "KG")]
        };

        await vm.LoadSalesCommand.ExecuteAsync(null);

        vm.SalesTransactions.Should().ContainSingle().Which.Should().Be(transaction);
        vm.HasSalesTransactions.Should().BeTrue();
        vm.HasCalculationResult.Should().BeFalse();
        vm.MaterialRequirements.Should().BeEmpty();
        salesRepo.VerifyAll();
    }

    [Fact]
    public void IsPerDocument_WhenSelected_UpdatesSaveModeWithoutBindingInversion()
    {
        var vm = new SalesCalculationViewModel(
            Mock.Of<ICalculateSalesProductionUseCase>(),
            Mock.Of<IErpSalesOrderRepository>(),
            Mock.Of<IDialogService>());

        vm.IsPerDocument = true;

        vm.IsDaily.Should().BeFalse();
        vm.IsPerDocument.Should().BeTrue();
    }

    [Fact]
    public async Task SaveDocumentsCommand_WhenSaveSucceeds_ShowsDocumentNumberAndClearsScreen()
    {
        var savedDocument = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 5, 25),
            DocNo: "BP-20260525-00001",
            DocTime: new TimeOnly(9, 0, 0),
            Orders: [],
            Details: []);

        var useCase = new Mock<ICalculateSalesProductionUseCase>();
        useCase
            .Setup(s => s.SaveAsync(
                It.Is<CalculateSalesProductionRequest>(r =>
                    r.DateFrom == new DateOnly(2026, 5, 1) &&
                    r.DateTo == new DateOnly(2026, 5, 25) &&
                    r.Mode == SaveMode.Daily &&
                    !r.DryRun),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<BomProductionDto>>.Success([savedDocument]));

        var dialogService = new Mock<IDialogService>();
        dialogService
            .Setup(s => s.AlertAsync(
                "บันทึกสำเร็จ",
                $"บันทึกเข้าสู่เลขที่เอกสาร {savedDocument.DocNo} สำเร็จแล้ว"))
            .Returns(Task.CompletedTask);

        var vm = new SalesCalculationViewModel(
            useCase.Object,
            Mock.Of<IErpSalesOrderRepository>(),
            dialogService.Object)
        {
            DateFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            DateTo = new DateTimeOffset(2026, 5, 25, 0, 0, 0, TimeSpan.Zero),
            HasCalculationResult = true,
            ShowOnlyWithBom = true,
            ItemsWithoutBomCount = 2
        };
        vm.SalesTransactions = [new ErpSalesTransactionDto(
            DocDate: new DateOnly(2026, 5, 1),
            DocNo: "SI2605-00001",
            ItemCode: "FG-001",
            ItemName: "Finished Good 001",
            Qty: 5m,
            UnitCode: "PCS",
            StandValue: 1m,
            DivideValue: 1m)];
        vm.MaterialRequirements = [new MaterialRequirementDto(
            MaterialCode: "MAT-001",
            MaterialName: "Material 001",
            RequiredQty: 3m,
            Unit: "KG")];

        await vm.SaveDocumentsCommand.ExecuteAsync(null);

        dialogService.Verify(s => s.AlertAsync(
            "บันทึกสำเร็จ",
            $"บันทึกเข้าสู่เลขที่เอกสาร {savedDocument.DocNo} สำเร็จแล้ว"), Times.Once);
        vm.DateFrom.Should().BeNull();
        vm.DateTo.Should().BeNull();
        vm.SalesTransactions.Should().BeEmpty();
        vm.MaterialRequirements.Should().BeEmpty();
        vm.HasCalculationResult.Should().BeFalse();
        vm.ShowOnlyWithBom.Should().BeFalse();
        vm.ItemsWithoutBomCount.Should().Be(0);
        vm.HasError.Should().BeFalse();
    }
}
