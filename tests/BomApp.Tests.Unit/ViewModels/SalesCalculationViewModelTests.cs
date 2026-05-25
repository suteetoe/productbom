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
        vm.SalesTransactions.Add(new ErpSalesTransactionDto(
            DocDate: new DateOnly(2026, 5, 1),
            DocNo: "SI2605-00001",
            ItemCode: "FG-001",
            Qty: 5m,
            UnitCode: "PCS",
            StandValue: 1m,
            DivideValue: 1m));
        vm.MaterialRequirements.Add(new MaterialRequirementDto(
            MaterialCode: "MAT-001",
            MaterialName: "Material 001",
            RequiredQty: 3m,
            Unit: "KG"));

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
