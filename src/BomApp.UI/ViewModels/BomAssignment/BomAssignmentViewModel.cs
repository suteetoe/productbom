using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;

namespace BomApp.UI.ViewModels.BomAssignment;

/// <summary>
/// Local row model — ERP item augmented with assignment status.
/// IsAssigned and AssignedBomId are refreshed lazily on row selection.
/// </summary>
public record ErpItemRow(string ItemCode, string ItemName, string? Category, bool IsAssigned, Guid? AssignedBomId);

public partial class BomAssignmentViewModel : ViewModelBase
{
    private readonly IBomAssignmentService _assignmentService;
    private readonly IErpItemRepository _erpItemRepository;

    // ── Left panel ──────────────────────────────────────────────────────────

    /// <summary>Bound to search TextBox; filters FilteredItems on change.</summary>
    [ObservableProperty] private string _itemSearchText = string.Empty;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _errorMessage = string.Empty;

    /// <summary>Computed from ErrorMessage — drives error banner visibility.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    /// <summary>Full list loaded from IErpItemRepository.</summary>
    public ObservableCollection<ErpItemRow> ErpItems { get; } = new();

    /// <summary>
    /// Client-side filter — searches ItemCode and ItemName (case-insensitive).
    /// Re-evaluated whenever ItemSearchText changes.
    /// </summary>
    public IEnumerable<ErpItemRow> FilteredItems => string.IsNullOrWhiteSpace(ItemSearchText)
        ? ErpItems
        : ErpItems.Where(i =>
            i.ItemCode.Contains(ItemSearchText, StringComparison.OrdinalIgnoreCase) ||
            i.ItemName.Contains(ItemSearchText, StringComparison.OrdinalIgnoreCase));

    partial void OnItemSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredItems));

    // ── Right panel ─────────────────────────────────────────────────────────

    /// <summary>
    /// Currently selected ERP item row.
    /// On change triggers RefreshRightPanelAsync to load its BOM assignment.
    /// </summary>
    [ObservableProperty] private ErpItemRow? _selectedItem;

    /// <summary>BOM currently assigned to SelectedItem (null = none).</summary>
    [ObservableProperty] private BomDto? _assignedBom;

    /// <summary>BOM the user has chosen in the assignment ComboBox.</summary>
    [ObservableProperty] private BomDto? _selectedBomToAssign;

    /// <summary>Active BOMs available for assignment — drives the ComboBox.</summary>
    public ObservableCollection<BomDto> ActiveBoms { get; } = new();

    /// <summary>Computed — controls visibility of the "ถอดสูตร" button.</summary>
    public bool HasAssignment => AssignedBom is not null;

    partial void OnAssignedBomChanged(BomDto? value) => OnPropertyChanged(nameof(HasAssignment));

    // ── Constructor ─────────────────────────────────────────────────────────

    public BomAssignmentViewModel(IBomAssignmentService assignmentService, IErpItemRepository erpItemRepository)
    {
        _assignmentService = assignmentService;
        _erpItemRepository = erpItemRepository;
    }

    // ── Reactive handlers ────────────────────────────────────────────────────

    partial void OnSelectedItemChanged(ErpItemRow? value) => _ = RefreshRightPanelAsync(value);

    /// <summary>
    /// Loads the BOM assignment for the given row and updates the row's
    /// IsAssigned indicator in ErpItems so the left DataGrid badge refreshes.
    /// </summary>
    private async Task RefreshRightPanelAsync(ErpItemRow? row)
    {
        if (row is null)
        {
            AssignedBom = null;
            return;
        }

        var result = await _assignmentService.GetAssignedBomAsync(row.ItemCode);
        AssignedBom = result.IsSuccess ? result.Value : null;

        // Update the IsAssigned flag on the row in the ObservableCollection
        // so the status badge in the DataGrid reflects the current state.
        var idx = ErpItems.IndexOf(row);
        if (idx >= 0)
        {
            ErpItems[idx] = row with { IsAssigned = AssignedBom is not null, AssignedBomId = AssignedBom?.Id };
            OnPropertyChanged(nameof(FilteredItems));
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads ERP items and active BOMs in parallel.
    /// Assignment status is resolved lazily when a row is selected.
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var itemsTask = _erpItemRepository.GetAllItemsAsync();
            var bomsTask  = _assignmentService.GetActiveBomsAsync();
            await Task.WhenAll(itemsTask, bomsTask);

            var erpItems  = await itemsTask;
            var bomsResult = await bomsTask;

            // Populate Active BOMs dropdown
            ActiveBoms.Clear();
            if (bomsResult.IsSuccess)
                foreach (var b in bomsResult.Value!) ActiveBoms.Add(b);

            // Populate ERP items list; assignment status starts unknown (false)
            // and is resolved lazily when the user selects a row.
            ErpItems.Clear();
            foreach (var item in erpItems)
                ErpItems.Add(new ErpItemRow(item.Code, item.Name, null, false, null));

            OnPropertyChanged(nameof(FilteredItems));
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Assigns SelectedBomToAssign to SelectedItem via IBomAssignmentService,
    /// then refreshes the right panel to reflect the new assignment.
    /// </summary>
    [RelayCommand]
    private async Task AssignBomAsync()
    {
        if (SelectedItem is null || SelectedBomToAssign is null)
        {
            ErrorMessage = "กรุณาเลือกสินค้าและสูตรการผลิต";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _assignmentService.AssignAsync(
                SelectedItem.ItemCode,
                SelectedItem.ItemName,
                SelectedBomToAssign.Id,
                "current-user");

            if (result.IsSuccess)
                await RefreshRightPanelAsync(SelectedItem);
            else
                ErrorMessage = result.Error ?? "ไม่สามารถกำหนดสูตรได้";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Removes the BOM assignment from SelectedItem,
    /// then refreshes the right panel to show the unassigned state.
    /// </summary>
    [RelayCommand]
    private async Task RemoveAssignmentAsync()
    {
        if (SelectedItem is null) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _assignmentService.RemoveAsync(SelectedItem.ItemCode);

            if (result.IsSuccess)
                await RefreshRightPanelAsync(SelectedItem);
            else
                ErrorMessage = result.Error ?? "ไม่สามารถถอดสูตรได้";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
