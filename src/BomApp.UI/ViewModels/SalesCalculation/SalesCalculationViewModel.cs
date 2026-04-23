using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;

namespace BomApp.UI.ViewModels.SalesCalculation;

public partial class SalesCalculationViewModel : ViewModelBase
{
    private readonly ICalculateSalesProductionUseCase _useCase;
    private readonly IErpSalesOrderRepository _salesRepo;

    // ── State ────────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _errorMessage = string.Empty;

    /// <summary>Drives error banner visibility.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    // ── Filter / options ─────────────────────────────────────────────────────

    /// <summary>Bound to the "วันที่เริ่มต้น" DatePicker.</summary>
    [ObservableProperty] private DateTimeOffset? _dateFrom;

    /// <summary>Bound to the "ถึงวันที่" DatePicker.</summary>
    [ObservableProperty] private DateTimeOffset? _dateTo;

    /// <summary>
    /// true  → SaveMode.Daily (รวมเป็น 1 เอกสารต่อ 1 วัน).
    /// false → SaveMode.PerDocument (แยกตามเลขที่เอกสารขาย).
    /// </summary>
    [ObservableProperty] private bool _isDaily = true;

    /// <summary>Toggles "show only items with BOM" filter in the sales DataGrid.</summary>
    [ObservableProperty] private bool _showOnlyWithBom;

    /// <summary>Number of items skipped in the last calculation due to missing BOM.</summary>
    [ObservableProperty] private int _itemsWithoutBomCount;

    /// <summary>True when at least one item was skipped — drives warning badge visibility.</summary>
    public bool HasWarning => ItemsWithoutBomCount > 0;

    partial void OnItemsWithoutBomCountChanged(int value) => OnPropertyChanged(nameof(HasWarning));

    // ── Data ─────────────────────────────────────────────────────────────────

    /// <summary>Raw ERP sales transactions loaded by LoadSalesAsync.</summary>
    public ObservableCollection<ErpSalesTransactionDto> SalesTransactions { get; } = new();

    /// <summary>True when SalesTransactions has entries — shows DataGrid, hides empty state.</summary>
    public bool HasSalesTransactions => SalesTransactions.Count > 0;

    /// <summary>Material requirements produced by the last CalculateAsync call.</summary>
    public ObservableCollection<MaterialRequirementDto> MaterialRequirements { get; } = new();

    /// <summary>
    /// Becomes true after a successful CalculateAsync.
    /// Used as CanExecute for SaveDocumentsCommand and controls Save button IsEnabled.
    /// </summary>
    [ObservableProperty] private bool _hasCalculationResult;

    partial void OnHasCalculationResultChanged(bool value)
        => SaveDocumentsCommand.NotifyCanExecuteChanged();

    // ── Constructor ──────────────────────────────────────────────────────────

    public SalesCalculationViewModel(ICalculateSalesProductionUseCase useCase, IErpSalesOrderRepository salesRepo)
    {
        _useCase   = useCase;
        _salesRepo = salesRepo;
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads ERP sales transactions for the selected date range.
    /// Clears any previous calculation result.
    /// </summary>
    [RelayCommand]
    private async Task LoadSalesAsync()
    {
        if (DateFrom is null || DateTo is null)
        {
            ErrorMessage = "กรุณาเลือกช่วงวันที่";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var from = DateOnly.FromDateTime(DateFrom.Value.DateTime);
            var to   = DateOnly.FromDateTime(DateTo.Value.DateTime);

            var txns = await _salesRepo.GetSalesTransactionsByDateRangeAsync(from, to);

            SalesTransactions.Clear();
            foreach (var t in txns) SalesTransactions.Add(t);
            OnPropertyChanged(nameof(HasSalesTransactions));

            // Reset calculation state so Save is disabled until CalculateAsync runs again
            HasCalculationResult = false;
            MaterialRequirements.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Runs DryRun=true calculation and populates MaterialRequirements.
    /// Sets HasCalculationResult=true on success, enabling SaveDocumentsCommand.
    /// </summary>
    [RelayCommand]
    private async Task CalculateAsync()
    {
        if (DateFrom is null || DateTo is null)
        {
            ErrorMessage = "กรุณาเลือกช่วงวันที่ก่อนคำนวณ";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var request = BuildRequest(dryRun: true);
            var result  = await _useCase.CalculateAsync(request);

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "เกิดข้อผิดพลาดในการคำนวณ";
                return;
            }

            var dto = result.Value!;
            ItemsWithoutBomCount = dto.SkippedItemCount;

            MaterialRequirements.Clear();
            foreach (var m in dto.Materials) MaterialRequirements.Add(m);

            HasCalculationResult = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Saves production documents (DryRun=false).
    /// Only enabled after a successful CalculateAsync (HasCalculationResult=true).
    /// On success, shows the number of documents created in ErrorMessage (info).
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasCalculationResult))]
    private async Task SaveDocumentsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var request = BuildRequest(dryRun: false);
            var result  = await _useCase.SaveAsync(request);

            if (!result.IsSuccess)
                ErrorMessage = result.Error ?? "บันทึกไม่สำเร็จ";
            else
                ErrorMessage = $"บันทึกสำเร็จ {result.Value!.Count} เอกสาร";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Exports MaterialRequirements to CSV.
    /// Sprint 4: implement file save dialog + CSV write.
    /// </summary>
    [RelayCommand]
    private void ExportCsv()
    {
        // TODO Sprint 4: open save dialog and write MaterialRequirements to CSV
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private CalculateSalesProductionRequest BuildRequest(bool dryRun) => new(
        DateFrom:   DateOnly.FromDateTime(DateFrom!.Value.DateTime),
        DateTo:     DateOnly.FromDateTime(DateTo!.Value.DateTime),
        Mode:       IsDaily ? SaveMode.Daily : SaveMode.PerDocument,
        DryRun:     dryRun,
        CreatedBy:  "current-user",
        CreatedVia: "UI");
}
