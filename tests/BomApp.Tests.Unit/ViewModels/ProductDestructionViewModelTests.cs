using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;
using BomApp.UI.ViewModels.ProductDestruction;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Unit.ViewModels;

public class ProductDestructionViewModelTests
{
    [Fact]
    public async Task LoadInitialCommand_WhenScreenOpens_LoadsProductDestructionDocuments()
    {
        var document = new ProductDestructionDto(
            DocNo: "PD-20260616-00001",
            DocDate: new DateOnly(2026, 6, 16),
            WhCode: "WH01",
            ShelfCode: "A01",
            Remark: "damaged",
            Pictures: [],
            Details: []);

        var service = new Mock<IProductDestructionService>();
        service
            .Setup(s => s.GetDocumentsPageAsync(
                It.Is<ProductDestructionListQuery>(q =>
                    q.DocDateFrom == null &&
                    q.DocDateTo == null &&
                    q.DocNo == null &&
                    q.PageNumber == 1 &&
                    q.PageSize == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<ProductDestructionDto>>.Success(new PagedResult<ProductDestructionDto>([document], 21, 1, 20)));

        var vm = new ProductDestructionViewModel(service.Object, Mock.Of<IErpItemRepository>());

        await vm.LoadInitialCommand.ExecuteAsync(null);

        vm.Documents.Should().ContainSingle().Which.Should().Be(document);
        vm.TotalCount.Should().Be(21);
        vm.TotalPages.Should().Be(2);
        vm.CanGoNext.Should().BeTrue();
        vm.CanGoPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task SaveCommand_WhenCreatingDocument_SendsHeaderPicturesAndDetails()
    {
        CreateProductDestructionCommand? captured = null;
        var saved = new ProductDestructionDto(
            DocNo: "PD-20260616-00001",
            DocDate: new DateOnly(2026, 6, 16),
            WhCode: "WH01",
            ShelfCode: "A01",
            Remark: "damaged",
            Pictures: [],
            Details: []);

        var service = new Mock<IProductDestructionService>();
        service
            .Setup(s => s.CreateAsync(It.IsAny<CreateProductDestructionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CreateProductDestructionCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(Result<ProductDestructionDto>.Success(saved));
        service
            .Setup(s => s.GetDocumentsPageAsync(It.IsAny<ProductDestructionListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<ProductDestructionDto>>.Success(new PagedResult<ProductDestructionDto>([saved], 1, 1, 20)));

        var vm = new ProductDestructionViewModel(service.Object, Mock.Of<IErpItemRepository>());
        vm.NewDocumentCommand.Execute(null);
        vm.DocDate = new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero);
        vm.DocNo = saved.DocNo;
        vm.WhCode = "WH01";
        vm.ShelfCode = "A01";
        vm.Remark = "damaged";
        vm.Details.Clear();
        vm.Details.Add(new ProductDestructionDetailEditModel
        {
            LineNumber = 1,
            ItemCode = "FG-001",
            ItemName = "Finished Good",
            Qty = 2,
            UnitCode = "PCS",
            WhCode = "WH01",
            ShelfCode = "A01"
        });
        vm.Pictures.Add(new ProductDestructionPictureEditModel
        {
            LineNumber = 1,
            ImageGuid = "image-1",
            ImageFile = [1, 2, 3]
        });

        await vm.SaveCommand.ExecuteAsync(null);

        captured.Should().NotBeNull();
        captured!.DocNo.Should().Be(saved.DocNo);
        captured.Details.Should().ContainSingle().Which.ItemCode.Should().Be("FG-001");
        captured.Pictures.Should().ContainSingle().Which.ImageFile.Should().Equal(1, 2, 3);
        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void CalendarDocDate_WhenChanged_UpdatesDocDateWithoutTimeConversionError()
    {
        var vm = new ProductDestructionViewModel(Mock.Of<IProductDestructionService>(), Mock.Of<IErpItemRepository>())
        {
            DocDate = new DateTimeOffset(2026, 6, 16, 10, 30, 0, TimeSpan.FromHours(7))
        };

        vm.CalendarDocDate = new DateTime(2026, 6, 17);

        vm.DocDate.Should().Be(new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.FromHours(7)));
        vm.CalendarDocDate.Should().Be(new DateTime(2026, 6, 17));
    }
}
