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

    public ObservableCollection<BomProductionDto> Documents { get; } = new();
    public ObservableCollection<BomProductionOrderDto> SelectedDocumentDetails { get; } = new();
    public ObservableCollection<BomProductionDetailDto> MaterialUsageRows { get; } = new();

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasDocuments => Documents.Count > 0;
    public bool HasSelectedDocument => SelectedDocument is not null;
    public int DocumentListColumnSpan => HasSelectedDocument ? 1 : 3;

    public ProductionListViewModel(IProductionService productionService, IDialogService dialogService)
    {
        _productionService = productionService;
        _dialogService = dialogService;
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

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
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var dateFrom = DateFrom.HasValue ? DateOnly.FromDateTime(DateFrom.Value.DateTime) : (DateOnly?)null;
            var dateTo = DateTo.HasValue ? DateOnly.FromDateTime(DateTo.Value.DateTime) : (DateOnly?)null;

            var result = await _productionService.GetDocumentsAsync(
                docDateFrom: dateFrom,
                docDateTo: dateTo,
                docNo: string.IsNullOrWhiteSpace(DocNoSearch) ? null : DocNoSearch,
                itemCode: string.IsNullOrWhiteSpace(ItemSearch) ? null : ItemSearch);

            Documents.Clear();
            SelectedDocument = null;
            SelectedDocumentDetails.Clear();
            MaterialUsageRows.Clear();

            if (result.IsSuccess)
            {
                foreach (var document in result.Value!) Documents.Add(document);
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
}
