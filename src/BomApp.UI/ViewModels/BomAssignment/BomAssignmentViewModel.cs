using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;

namespace BomApp.UI.ViewModels.BomAssignment;

// Mutable class (not record) so IsAssigned can be updated in-place —
// replacing the object in the collection would make the DataGrid lose selection.
public partial class ErpItemRow : ObservableObject
{
    public string ItemCode { get; }
    public string ItemName { get; }
    public string? Category { get; }
    [ObservableProperty] private bool _isAssigned;
    [ObservableProperty] private Guid? _assignedBomId;

    public ErpItemRow(string itemCode, string itemName, string? category, bool isAssigned, Guid? assignedBomId)
    {
        ItemCode = itemCode;
        ItemName = itemName;
        Category = category;
        _isAssigned = isAssigned;
        _assignedBomId = assignedBomId;
    }
}

public partial class BomAssignmentViewModel : ViewModelBase
{
    private readonly IBomAssignmentService _assignmentService;
    private readonly IErpItemRepository _erpItemRepository;

    // ── Left panel ──────────────────────────────────────────────────────────

    /// <summary>Bound to search TextBox; filters FilteredItems on change.</summary>
    [ObservableProperty] private string _itemSearchText = string.Empty;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _errorMessage = string.Empty;

    [ObservableProperty] private int _pageNumber = 1;

    [ObservableProperty] private int _pageSize = 20;

    [ObservableProperty] private int _totalCount;

    [ObservableProperty] private int _totalPages = 1;

    /// <summary>Computed from ErrorMessage — drives error banner visibility.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    public bool CanGoPrevious => PageNumber > 1 && !IsLoading;

    public bool CanGoNext => PageNumber < TotalPages && !IsLoading;

    public string PageSummary => $"หน้า {PageNumber} / {TotalPages} ({TotalCount} รายการ)";

    public IReadOnlyList<int> PageSizeOptions { get; } = [10, 20, 50, 100];

    /// <summary>Current ERP item page loaded from IErpItemRepository.</summary>
    public ObservableCollection<ErpItemRow> ErpItems { get; } = new();

    /// <summary>
    /// Current page rows. Search is handled by the repository so the total count stays correct.
    /// </summary>
    public IEnumerable<ErpItemRow> FilteredItems => ErpItems;

    partial void OnItemSearchTextChanged(string value)
    {
        PageNumber = 1;
        _ = LoadAsync();
    }

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
        _ = LoadAsync();
    }

    partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(PageSummary));

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageSummary));
    }

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

    /// <summary>Active BOMs available for assignment — full list loaded once.</summary>
    public ObservableCollection<BomDto> ActiveBoms { get; } = new();

    /// <summary>BOMs filtered to the selected product's ItemCode — drives the ComboBox.</summary>
    public IEnumerable<BomDto> FilteredBoms => SelectedItem is null
        ? Enumerable.Empty<BomDto>()
        : ActiveBoms.Where(b => b.ItemCode == SelectedItem.ItemCode);

    /// <summary>Computed — controls visibility of the assigned BOM detail and "ถอดสูตร" button.</summary>
    public bool HasAssignment => AssignedBom is not null;

    /// <summary>Computed — controls visibility of the BOM selector and "กำหนดสูตร" button.</summary>
    public bool CanAssignNew => SelectedItem is not null && !HasAssignment;

    partial void OnAssignedBomChanged(BomDto? value)
    {
        OnPropertyChanged(nameof(HasAssignment));
        OnPropertyChanged(nameof(CanAssignNew));
    }

    partial void OnSelectedItemChanged(ErpItemRow? value)
    {
        OnPropertyChanged(nameof(CanAssignNew));
        OnPropertyChanged(nameof(FilteredBoms));
        _ = RefreshRightPanelAsync(value);
    }

    // ── Constructor ─────────────────────────────────────────────────────────

    public BomAssignmentViewModel(IBomAssignmentService assignmentService, IErpItemRepository erpItemRepository)
    {
        _assignmentService = assignmentService;
        _erpItemRepository = erpItemRepository;
    }

    // ── Reactive handlers ────────────────────────────────────────────────────

    private async Task RefreshRightPanelAsync(ErpItemRow? row)
    {
        if (row is null)
        {
            AssignedBom = null;
            return;
        }

        var result = await _assignmentService.GetAssignedBomAsync(row.ItemCode);
        AssignedBom = result.IsSuccess ? result.Value : null;

        SelectedBomToAssign = AssignedBom is not null
            ? ActiveBoms.FirstOrDefault(b => b.Id == AssignedBom.Id)
            : null;

        // Mutate the row in-place — no collection Replace, so the DataGrid
        // never loses the selected item reference and focus stays put.
        row.IsAssigned = AssignedBom is not null;
        row.AssignedBomId = AssignedBom?.Id;
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads ERP item page and active BOMs in parallel.
    /// Assignment status is resolved lazily when a row is selected.
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var itemsTask = _erpItemRepository.GetItemsPageAsync(new ErpItemListQuery(ItemSearchText, PageNumber, PageSize));
            var bomsTask  = _assignmentService.GetActiveBomsAsync();
            await Task.WhenAll(itemsTask, bomsTask);

            var erpItemsPage = await itemsTask;
            var bomsResult = await bomsTask;

            // Populate Active BOMs dropdown
            ActiveBoms.Clear();
            if (bomsResult.IsSuccess)
                foreach (var b in bomsResult.Value!) ActiveBoms.Add(b);

            TotalCount = erpItemsPage.TotalCount;
            TotalPages = erpItemsPage.TotalPages;
            PageNumber = Math.Min(erpItemsPage.PageNumber, TotalPages);
            PageSize = erpItemsPage.PageSize;

            // Populate ERP items list; assignment status starts unknown (false)
            // and is resolved lazily when the user selects a row.
            var previousItemCode = SelectedItem?.ItemCode;
            SelectedItem = null;
            ErpItems.Clear();
            foreach (var item in erpItemsPage.Items)
                ErpItems.Add(new ErpItemRow(item.Code, item.Name, null, false, null));

            SelectedItem = ErpItems.FirstOrDefault(i => i.ItemCode == previousItemCode);
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

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (PageNumber <= 1)
            return;

        PageNumber--;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (PageNumber >= TotalPages)
            return;

        PageNumber++;
        await LoadAsync();
    }
}
