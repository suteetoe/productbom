using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Shared.Contracts;
using BomApp.UI.Services;

namespace BomApp.UI.ViewModels.Production;

public partial class ProductionListViewModel : ViewModelBase
{
    private readonly IProductionService _productionService;
    private readonly IDialogService _dialogService;
    private bool _hasLoadedInitialDocuments;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private DateTimeOffset? _dateFrom;
    [ObservableProperty] private DateTimeOffset? _dateTo;
    [ObservableProperty] private string _docNoSearch = string.Empty;
    [ObservableProperty] private string _itemSearch = string.Empty;
    [ObservableProperty] private BomProductionDto? _selectedDocument;
    [ObservableProperty] private int _pageNumber = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;

    public ObservableCollection<BomProductionDto> Documents { get; } = new();
    public ObservableCollection<BomProductionOrderDto> SelectedDocumentDetails { get; } = new();
    public ObservableCollection<BomProductionDetailDto> MaterialUsageRows { get; } = new();
    public IReadOnlyList<int> PageSizeOptions { get; } = [10, 20, 50, 100];

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasDocuments => Documents.Count > 0;
    public bool HasSelectedDocument => SelectedDocument is not null;
    public int DocumentListColumnSpan => HasSelectedDocument ? 1 : 3;
    public bool CanGoPrevious => PageNumber > 1 && !IsLoading;
    public bool CanGoNext => PageNumber < TotalPages && !IsLoading;
    public string PageSummary => $"หน้า {PageNumber} / {TotalPages} ({TotalCount} รายการ)";

    public ProductionListViewModel(IProductionService productionService, IDialogService dialogService)
    {
        _productionService = productionService;
        _dialogService = dialogService;
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    partial void OnPageNumberChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageSummary));
    }

    partial void OnPageSizeChanged(int value)
    {
        PageNumber = 1;
        _ = SearchAsync();
    }

    partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(PageSummary));

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageSummary));
    }

    partial void OnSelectedDocumentChanged(BomProductionDto? value)
    {
        OnPropertyChanged(nameof(HasSelectedDocument));
        OnPropertyChanged(nameof(DocumentListColumnSpan));
        _ = LoadDocumentDetailsAsync(value);
    }

    [RelayCommand]
    private async Task LoadInitialAsync()
    {
        if (_hasLoadedInitialDocuments) return;

        _hasLoadedInitialDocuments = true;
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var dateFrom = DateFrom.HasValue ? DateOnly.FromDateTime(DateFrom.Value.DateTime) : (DateOnly?)null;
            var dateTo = DateTo.HasValue ? DateOnly.FromDateTime(DateTo.Value.DateTime) : (DateOnly?)null;

            var result = await _productionService.GetDocumentsPageAsync(new BomProductionListQuery(
                DocDateFrom: dateFrom,
                DocDateTo: dateTo,
                DocNo: string.IsNullOrWhiteSpace(DocNoSearch) ? null : DocNoSearch,
                ItemCode: string.IsNullOrWhiteSpace(ItemSearch) ? null : ItemSearch,
                PageNumber: PageNumber,
                PageSize: PageSize));

            Documents.Clear();
            SelectedDocument = null;
            SelectedDocumentDetails.Clear();
            MaterialUsageRows.Clear();

            if (result.IsSuccess)
            {
                var page = result.Value!;
                TotalCount = page.TotalCount;
                TotalPages = page.TotalPages;
                PageNumber = Math.Min(page.PageNumber, TotalPages);
                PageSize = page.PageSize;

                foreach (var document in page.Items) Documents.Add(document);
                OnPropertyChanged(nameof(HasDocuments));
            }
            else
            {
                ErrorMessage = result.Error ?? "ไม่สามารถโหลดรายการผลิตได้";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectDocument(BomProductionDto document)
    {
        SelectedDocument = document;
    }

    [RelayCommand]
    private void ExportCsv(BomProductionDto document)
    {
        _ = document;
        // Sprint 4: open save dialog and write SelectedDocumentDetails to CSV.
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(BomProductionDto document)
    {
        var confirmed = await _dialogService.ConfirmAsync(
            "ยืนยันการลบ",
            $"ต้องการลบเอกสารผลิต '{document.DocNo}' ใช่หรือไม่?");
        if (!confirmed) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _productionService.DeleteDocumentAsync(document.DocNo);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "ไม่สามารถลบเอกสารผลิตได้";
                return;
            }

            Documents.Remove(document);
            if (SelectedDocument?.DocNo == document.DocNo)
            {
                SelectedDocument = null;
                SelectedDocumentDetails.Clear();
                MaterialUsageRows.Clear();
            }

            OnPropertyChanged(nameof(HasDocuments));
            TotalCount = Math.Max(0, TotalCount - 1);
            if (Documents.Count == 0 && PageNumber > 1)
                PageNumber--;

            await LoadPageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadDocumentDetailsAsync(BomProductionDto? document)
    {
        SelectedDocumentDetails.Clear();
        MaterialUsageRows.Clear();
        if (document is null) return;

        var orderResult = await _productionService.GetDocumentOrdersAsync(document.DocNo);
        var detailResult = await _productionService.GetDocumentDetailsAsync(document.DocNo);

        if (orderResult.IsSuccess && detailResult.IsSuccess)
        {
            foreach (var order in orderResult.Value!) SelectedDocumentDetails.Add(order);
            foreach (var detail in detailResult.Value!) MaterialUsageRows.Add(detail);
        }
        else
        {
            ErrorMessage = orderResult.Error ?? detailResult.Error ?? "ไม่สามารถโหลดรายละเอียดเอกสารได้";
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (PageNumber <= 1)
            return;

        PageNumber--;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (PageNumber >= TotalPages)
            return;

        PageNumber++;
        await LoadPageAsync();
    }
}
