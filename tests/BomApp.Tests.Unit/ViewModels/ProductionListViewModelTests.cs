using BomApp.Application.Interfaces;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;
using BomApp.UI.Services;
using BomApp.UI.ViewModels.Production;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Unit.ViewModels;

public class ProductionListViewModelTests
{
    [Fact]
    public async Task LoadInitialCommand_WhenScreenOpens_LoadsBomProductionDocuments()
    {
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 5, 23),
            DocNo: "BP-20260523-00001",
            DocTime: new TimeOnly(8, 0, 0),
            Details: []);

        var service = new Mock<IProductionService>();
        service
            .Setup(s => s.GetDocumentsAsync(
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<BomProductionDto>>.Success([document]));

        var vm = new ProductionListViewModel(service.Object, Mock.Of<IDialogService>());

        await vm.LoadInitialCommand.ExecuteAsync(null);

        vm.Documents.Should().ContainSingle().Which.Should().Be(document);
        vm.HasDocuments.Should().BeTrue();
        service.Verify(s => s.GetDocumentsAsync(
            null,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SelectedDocument_WhenChanged_LoadsBomProductionDetailsByDocNo()
    {
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 5, 23),
            DocNo: "BP-20260523-00001",
            DocTime: new TimeOnly(8, 0, 0),
            Details: []);

        var detail = new BomProductionDetailDto(
            Id: Guid.NewGuid(),
            DocNo: document.DocNo,
            ItemCode: "FG-001",
            Qty: 12m,
            UnitCode: "PCS");

        var service = new Mock<IProductionService>();
        service
            .Setup(s => s.GetDocumentDetailsAsync(document.DocNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<BomProductionDetailDto>>.Success([detail]));

        var vm = new ProductionListViewModel(service.Object, Mock.Of<IDialogService>());

        vm.SelectedDocument = document;
        await Task.Delay(50);

        vm.SelectedDocumentDetails.Should().ContainSingle().Which.Should().Be(detail);
        service.Verify(s => s.GetDocumentDetailsAsync(document.DocNo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentCommand_WhenDeleteSucceeds_RemovesDocumentFromList()
    {
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 5, 23),
            DocNo: "BP-20260523-00001",
            DocTime: new TimeOnly(8, 0, 0),
            Details: []);

        var service = new Mock<IProductionService>();
        service
            .Setup(s => s.DeleteDocumentAsync(document.DocNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var dialogService = new Mock<IDialogService>();
        dialogService
            .Setup(s => s.ConfirmAsync(
                "ยืนยันการลบ",
                $"ต้องการลบเอกสารผลิต '{document.DocNo}' ใช่หรือไม่?"))
            .ReturnsAsync(true);

        var vm = new ProductionListViewModel(service.Object, dialogService.Object);
        vm.Documents.Add(document);

        await vm.DeleteDocumentCommand.ExecuteAsync(document);

        vm.Documents.Should().BeEmpty();
        vm.HasDocuments.Should().BeFalse();
        vm.HasError.Should().BeFalse();
        dialogService.Verify(s => s.ConfirmAsync(
            "ยืนยันการลบ",
            $"ต้องการลบเอกสารผลิต '{document.DocNo}' ใช่หรือไม่?"), Times.Once);
        service.Verify(s => s.DeleteDocumentAsync(document.DocNo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentCommand_WhenConfirmationIsRejected_DoesNotDeleteDocument()
    {
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 5, 23),
            DocNo: "BP-20260523-00001",
            DocTime: new TimeOnly(8, 0, 0),
            Details: []);

        var service = new Mock<IProductionService>();
        var dialogService = new Mock<IDialogService>();
        dialogService
            .Setup(s => s.ConfirmAsync(
                "ยืนยันการลบ",
                $"ต้องการลบเอกสารผลิต '{document.DocNo}' ใช่หรือไม่?"))
            .ReturnsAsync(false);

        var vm = new ProductionListViewModel(service.Object, dialogService.Object);
        vm.Documents.Add(document);

        await vm.DeleteDocumentCommand.ExecuteAsync(document);

        vm.Documents.Should().ContainSingle().Which.Should().Be(document);
        service.Verify(s => s.DeleteDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
