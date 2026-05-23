using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Shared.Contracts;

namespace BomApp.UI.ViewModels.Production;

public partial class ProductionListViewModel : ViewModelBase
{
    private readonly IProductionService _productionService;
    private bool _hasLoadedInitialOrders;

    // ── State ────────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _errorMessage = string.Empty;

    /// <summary>Drives error banner visibility.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    // ── Filter properties ────────────────────────────────────────────────────

    /// <summary>Bound to the "จาก" DatePicker.</summary>
    [ObservableProperty] private DateTimeOffset? _dateFrom;

    /// <summary>Bound to the "ถึง" DatePicker.</summary>
    [ObservableProperty] private DateTimeOffset? _dateTo;

    /// <summary>
    /// Status filter — empty string means "ทั้งหมด".
    /// Valid values: Pending | Processing | Done | Cancelled.
    /// </summary>
    [ObservableProperty] private string _statusFilter = string.Empty;

    /// <summary>Free-text item code search.</summary>
    [ObservableProperty] private string _itemSearch = string.Empty;

    /// <summary>
    /// CreatedVia filter — empty string means "ทั้งหมด".
    /// Valid values: "UI" | "CLI".
    /// </summary>
    [ObservableProperty] private string _createdViaFilter = string.Empty;

    /// <summary>Free-text search inside source SO numbers.</summary>
    [ObservableProperty] private string _sourceDocNoSearch = string.Empty;

    // ── Data ─────────────────────────────────────────────────────────────────

    /// <summary>Production orders returned by the last SearchAsync call.</summary>
    public ObservableCollection<ProductionOrderDto> Orders { get; } = new();

    /// <summary>True when Orders has at least one entry — hides empty-state placeholder.</summary>
    public bool HasOrders => Orders.Count > 0;

    /// <summary>
    /// Currently selected order in the upper DataGrid.
    /// On change, triggers LoadOrderLinesAsync to populate the lower DataGrid.
    /// </summary>
    [ObservableProperty] private ProductionOrderDto? _selectedOrder;

    /// <summary>BOM lines of the selected order — bound to the lower DataGrid.</summary>
    public ObservableCollection<ProductionOrderLineDto> SelectedOrderLines { get; } = new();

    // ── Constructor ──────────────────────────────────────────────────────────

    public ProductionListViewModel(IProductionService productionService)
    {
        _productionService = productionService;
    }

    // ── Reactive handlers ────────────────────────────────────────────────────

    partial void OnSelectedOrderChanged(ProductionOrderDto? value) => _ = LoadOrderLinesAsync(value);

    /// <summary>Loads BOM lines for the given order into SelectedOrderLines.</summary>
    private async Task LoadOrderLinesAsync(ProductionOrderDto? order)
    {
        SelectedOrderLines.Clear();
        if (order is null) return;

        var result = await _productionService.GetOrderLinesAsync(order.Id);
        if (result.IsSuccess)
            foreach (var line in result.Value!) SelectedOrderLines.Add(line);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Loads the production list once when the screen is opened.</summary>
    [RelayCommand]
    private async Task LoadInitialAsync()
    {
        if (_hasLoadedInitialOrders) return;

        _hasLoadedInitialOrders = true;
        await SearchAsync();
    }

    /// <summary>
    /// Queries IProductionService.GetOrdersAsync with the current filter values.
    /// Converts DateTimeOffset? → DateOnly? before passing to the service.
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var dateFrom = DateFrom.HasValue ? DateOnly.FromDateTime(DateFrom.Value.DateTime) : (DateOnly?)null;
            var dateTo   = DateTo.HasValue   ? DateOnly.FromDateTime(DateTo.Value.DateTime)   : (DateOnly?)null;

            var result = await _productionService.GetOrdersAsync(
                dateFrom:    dateFrom,
                dateTo:      dateTo,
                status:      string.IsNullOrWhiteSpace(StatusFilter)     ? null : StatusFilter,
                itemCode:    string.IsNullOrWhiteSpace(ItemSearch)        ? null : ItemSearch,
                createdVia:  string.IsNullOrWhiteSpace(CreatedViaFilter)  ? null : CreatedViaFilter,
                sourceDocNo: string.IsNullOrWhiteSpace(SourceDocNoSearch) ? null : SourceDocNoSearch);

            Orders.Clear();
            if (result.IsSuccess)
            {
                foreach (var o in result.Value!) Orders.Add(o);
                OnPropertyChanged(nameof(HasOrders));
            }
            else
            {
                ErrorMessage = result.Error ?? "เกิดข้อผิดพลาด";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Cancels the given production order.
    /// CommandParameter="{Binding}" passes the row's ProductionOrderDto.
    /// Refreshes the list on success.
    /// </summary>
    [RelayCommand]
    private async Task CancelOrderAsync(ProductionOrderDto order)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            // CancelProductionOrderCommand(Guid OrderId, string Reason)
            var result = await _productionService.CancelOrderAsync(
                new CancelProductionOrderCommand(order.Id, "ยกเลิกโดย current-user"));

            if (result.IsSuccess)
                await SearchAsync();
            else
                ErrorMessage = result.Error ?? "ยกเลิกไม่สำเร็จ";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Exports the given order's lines to CSV.
    /// CommandParameter="{Binding}" passes the row's ProductionOrderDto.
    /// Sprint 4: implement file save dialog + CSV write.
    /// </summary>
    [RelayCommand]
    private void ExportCsv(ProductionOrderDto order)
    {
        // TODO Sprint 4: open save dialog and write SelectedOrderLines to CSV
    }
}
