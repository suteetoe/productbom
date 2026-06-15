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
            Orders: [],
            Details: []);

        var service = new Mock<IProductionService>();
        service
            .Setup(s => s.GetDocumentsPageAsync(
                It.Is<BomProductionListQuery>(q =>
                    q.DocDateFrom == null &&
                    q.DocDateTo == null &&
                    q.DocNo == null &&
                    q.ItemCode == null &&
                    q.PageNumber == 1 &&
                    q.PageSize == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<BomProductionDto>>.Success(new PagedResult<BomProductionDto>([document], 25, 1, 20)));

        var vm = new ProductionListViewModel(service.Object, Mock.Of<IDialogService>());

        await vm.LoadInitialCommand.ExecuteAsync(null);

        vm.Documents.Should().ContainSingle().Which.Should().Be(document);
        vm.HasDocuments.Should().BeTrue();
        vm.TotalCount.Should().Be(25);
        vm.TotalPages.Should().Be(2);
        vm.CanGoNext.Should().BeTrue();
        vm.CanGoPrevious.Should().BeFalse();
        service.Verify(s => s.GetDocumentsPageAsync(
            It.Is<BomProductionListQuery>(q => q.PageNumber == 1 && q.PageSize == 20),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NextPageCommand_WhenMorePages_LoadsNextProductionDocumentPage()
    {
        var firstPageDocument = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 5, 23),
            DocNo: "BP-20260523-00001",
            DocTime: new TimeOnly(8, 0, 0),
            Orders: [],
            Details: []);
        var secondPageDocument = firstPageDocument with
        {
            Id = Guid.NewGuid(),
            DocNo = "BP-20260523-00021"
        };

        var service = new Mock<IProductionService>();
        service
            .Setup(s => s.GetDocumentsPageAsync(
                It.Is<BomProductionListQuery>(q => q.PageNumber == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<BomProductionDto>>.Success(new PagedResult<BomProductionDto>([firstPageDocument], 25, 1, 20)));
        service
            .Setup(s => s.GetDocumentsPageAsync(
                It.Is<BomProductionListQuery>(q => q.PageNumber == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<BomProductionDto>>.Success(new PagedResult<BomProductionDto>([secondPageDocument], 25, 2, 20)));

        var vm = new ProductionListViewModel(service.Object, Mock.Of<IDialogService>());

        await vm.LoadInitialCommand.ExecuteAsync(null);
        await vm.NextPageCommand.ExecuteAsync(null);

        vm.PageNumber.Should().Be(2);
        vm.Documents.Should().ContainSingle().Which.Should().Be(secondPageDocument);
        vm.CanGoNext.Should().BeFalse();
        vm.CanGoPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task SelectedDocument_WhenChanged_LoadsBomProductionDetailsByDocNo()
    {
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2026, 5, 23),
            DocNo: "BP-20260523-00001",
            DocTime: new TimeOnly(8, 0, 0),
            Orders: [],
            Details: []);

        var order = new BomProductionOrderDto(
            Id: Guid.NewGuid(),
            DocNo: document.DocNo,
            DocDate: document.DocDate,
            RefDocNo: "SO-20260523-00001",
            RefDocDate: document.DocDate,
            ItemCode: "FG-001",
            ItemName: "Finished Good 001",
            Qty: 12m,
            UnitCode: "PCS");
        var detail = new BomProductionDetailDto(
            Id: Guid.NewGuid(),
            DocNo: document.DocNo,
            ItemCode: "MAT-001",
            ItemName: "Material 001",
            Qty: 3m,
            UnitCode: "KG");

        var service = new Mock<IProductionService>();
        service
            .Setup(s => s.GetDocumentOrdersAsync(document.DocNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<BomProductionOrderDto>>.Success([order]));
        service
            .Setup(s => s.GetDocumentDetailsAsync(document.DocNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<BomProductionDetailDto>>.Success([detail]));

        var vm = new ProductionListViewModel(service.Object, Mock.Of<IDialogService>());

        vm.HasSelectedDocument.Should().BeFalse();
        vm.DocumentListColumnSpan.Should().Be(3);

        vm.SelectedDocument = document;
        await Task.Delay(50);

        vm.HasSelectedDocument.Should().BeTrue();
        vm.DocumentListColumnSpan.Should().Be(1);
        vm.SelectedDocumentDetails.Should().ContainSingle().Which.Should().Be(order);
        vm.SelectedDocumentDetails.Single().ItemName.Should().Be("Finished Good 001");
        vm.MaterialUsageRows.Should().ContainSingle().Which.Should().Be(detail);
        service.Verify(s => s.GetDocumentOrdersAsync(document.DocNo, It.IsAny<CancellationToken>()), Times.Once);
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
            Orders: [],
            Details: []);

        var service = new Mock<IProductionService>();
        service
            .Setup(s => s.DeleteDocumentAsync(document.DocNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        service
            .Setup(s => s.GetDocumentsPageAsync(
                It.IsAny<BomProductionListQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<BomProductionDto>>.Success(new PagedResult<BomProductionDto>([], 0, 1, 20)));
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
            Orders: [],
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
