using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Common;
using BomApp.Shared.Contracts;
using BomApp.UI.Services;

namespace BomApp.UI.ViewModels.Bom;

public partial class BomEditorViewModel : ViewModelBase
{
    private readonly IBomService _bomService;
    private readonly INavigationService _navigation;
    private readonly IErpItemRepository _erpRepo;

    // Set by BomEditorView code-behind — opens the product search dialog
    public Func<Task<ErpItemDto?>>? ShowProductSearchDialog { get; set; }

    // ------------------------------------------------------------------ //
    // Header fields                                                        //
    // ------------------------------------------------------------------ //

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _itemCode = string.Empty;
    [ObservableProperty] private string _itemName = string.Empty;
    [ObservableProperty] private string _unitCost = string.Empty;

    /// <summary>
    /// Displayed in the TextBox.
    /// Shows "CODE~Name" when a product is selected; shows only CODE when focused for editing.
    /// Managed via RefreshItemSearchText() and CommitItemSearchTextAsync().
    /// </summary>
    [ObservableProperty] private string _itemSearchText = string.Empty;

    /// <summary>Guards against re-entrant updates from RefreshItemSearchText while CommitItemSearchTextAsync is running.</summary>
    private bool _suppressSearchTextUpdate;
    /// <summary>Prevents OnItemCodeChanged from firing a second concurrent lookup when CommitItemSearchTextAsync sets ItemCode.</summary>
    private bool _suppressLookup;

    public ObservableCollection<ErpUnitDto> AvailableUnits { get; } = new();
    [ObservableProperty] private ErpUnitDto? _selectedYieldUnit;

    partial void OnItemCodeChanged(string value)
    {
        if (!IsLoading && !_suppressLookup)
            _ = LookupItemNameAsync(value);
    }

    private async Task LookupItemNameAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            ItemName = string.Empty;
            UnitCost = string.Empty;
            AvailableUnits.Clear();
            SelectedYieldUnit = null;
            RefreshItemSearchText();
            return;
        }
        var item = await _erpRepo.GetItemByCodeAsync(code);
        ItemName = item?.Name ?? string.Empty;
        UnitCost = item?.UnitCost ?? string.Empty;

        AvailableUnits.Clear();
        SelectedYieldUnit = null;
        if (item is not null)
        {
            var units = await _erpRepo.GetUnitsByItemCodeAsync(code);
            foreach (var u in units)
                AvailableUnits.Add(u);
            SelectedYieldUnit = AvailableUnits.FirstOrDefault(u => u.Code == UnitCost);
        }
        RefreshItemSearchText();
    }

    /// <summary>
    /// Updates ItemSearchText to show "CODE~Name" (or just CODE when Name is empty).
    /// Skipped when _suppressSearchTextUpdate is true to avoid re-entrant loops.
    /// </summary>
    private void RefreshItemSearchText()
    {
        if (_suppressSearchTextUpdate) return;
        ItemSearchText = string.IsNullOrEmpty(ItemName)
            ? ItemCode
            : $"{ItemCode}~{ItemName}";
    }

    /// <summary>Returns only the item code portion, used by the GotFocus handler in code-behind.</summary>
    public string GetItemCodeOnly() => ItemCode;

    /// <summary>
    /// Called from code-behind on Enter key press in the product TextBox.
    /// Parses the raw text (strips "~Name" if present), updates ItemCode, triggers lookup, then refreshes the display text.
    /// </summary>
    public async Task CommitItemSearchTextAsync(string rawText)
    {
        var tildeIndex = rawText.IndexOf('~');
        var code = (tildeIndex >= 0 ? rawText[..tildeIndex] : rawText).Trim();

        _suppressSearchTextUpdate = true;
        _suppressLookup = true;
        ItemCode = code;
        _suppressLookup = false;
        _suppressSearchTextUpdate = false;

        await LookupItemNameAsync(code);
    }
    [ObservableProperty] private decimal _yieldQuantity = 1m;
    [ObservableProperty] private string _yieldUnit = string.Empty;
    [ObservableProperty] private string _status = "Draft";
    [ObservableProperty] private int _version = 1;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    public ObservableCollection<BomSelectionOption> AvailableSubBoms { get; } = new();

    // ------------------------------------------------------------------ //
    // Edit mode tracking                                                   //
    // ------------------------------------------------------------------ //

    /// <summary>null = creating new; non-null = editing existing BOM.</summary>
    public Guid? EditingId { get; private set; }

    /// <summary>True when editing an existing BOM; false when creating new.</summary>
    public bool IsEditing => EditingId.HasValue;

    /// <summary>
    /// Called by BomListViewModel (via NavigateTo configure-overload) to switch
    /// this editor into edit mode and pre-populate the form.
    /// </summary>
    public async Task LoadForEditAsync(Guid id)
    {
        EditingId = id;
        OnPropertyChanged(nameof(IsEditing));

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _bomService.GetByIdAsync(id);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "โหลดข้อมูลไม่สำเร็จ";
                return;
            }
            var bom = result.Value;
            Code        = bom.Code;
            Name        = bom.Name;
            Description = bom.Description ?? string.Empty;
            ItemCode    = bom.ItemCode;
            var erpItem = await _erpRepo.GetItemByCodeAsync(bom.ItemCode);
            ItemName    = erpItem?.Name ?? bom.ItemName;
            UnitCost    = erpItem?.UnitCost ?? string.Empty;
            var units = await _erpRepo.GetUnitsByItemCodeAsync(bom.ItemCode);
            AvailableUnits.Clear();
            foreach (var u in units) AvailableUnits.Add(u);
            YieldQuantity = bom.YieldQuantity;
            YieldUnit   = bom.YieldUnit;
            SelectedYieldUnit = AvailableUnits.FirstOrDefault(u => u.Code == bom.YieldUnit);
            RefreshItemSearchText();
            Status      = bom.Status;
            Version     = bom.Version;
            await LoadSubBomOptionsAsync();
            Lines.Clear();
            foreach (var l in bom.Lines)
            {
                var materialItem = string.IsNullOrWhiteSpace(l.MaterialName)
                    ? await _erpRepo.GetItemByCodeAsync(l.MaterialCode)
                    : null;
                var materialName = string.IsNullOrWhiteSpace(l.MaterialName)
                    ? materialItem?.Name ?? string.Empty
                    : l.MaterialName;
                var lineUnits = await _erpRepo.GetUnitsByItemCodeAsync(l.MaterialCode);
                var editLine = CreateLineModel();
                editLine.SortOrder = l.SortOrder;
                editLine.MaterialCode = l.MaterialCode;
                editLine.MaterialName = materialName;
                editLine.Quantity = l.Quantity;
                editLine.Unit = l.Unit;
                editLine.SubBomId = l.SubBomId;
                editLine.Notes = l.Notes ?? string.Empty;
                foreach (var u in lineUnits)
                    editLine.AvailableUnits.Add(u);
                editLine.SelectedUnit = editLine.AvailableUnits.FirstOrDefault(u => u.Code == l.Unit);
                editLine.RefreshSelectedSubBom();
                Lines.Add(editLine);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ------------------------------------------------------------------ //
    // BOM Lines                                                            //
    // ------------------------------------------------------------------ //

    /// <summary>BOM line rows bound to the inline-editable DataGrid.</summary>
    public ObservableCollection<BomLineEditModel> Lines { get; } = new();

    // ------------------------------------------------------------------ //
    // Constructor                                                          //
    // ------------------------------------------------------------------ //

    public BomEditorViewModel(IBomService bomService, INavigationService navigation, IErpItemRepository erpRepo)
    {
        _bomService = bomService;
        _navigation = navigation;
        _erpRepo = erpRepo;
        AvailableSubBoms.Add(BomSelectionOption.None);
    }

    // Called by View code-behind to provide the search function to the dialog
    public async Task<IReadOnlyList<ErpItemDto>> SearchItemsAsync(string keyword) =>
        string.IsNullOrWhiteSpace(keyword)
            ? await _erpRepo.GetAllItemsAsync()
            : await _erpRepo.SearchItemsAsync(keyword);

    // ------------------------------------------------------------------ //
    // Commands                                                             //
    // ------------------------------------------------------------------ //

    /// <summary>Open product search dialog then append the selected item as a BOM line.</summary>
    [RelayCommand]
    private async Task AddLineAsync()
    {
        if (ShowProductSearchDialog is null) return;
        var item = await ShowProductSearchDialog();
        if (item is null) return;

        var units = await _erpRepo.GetUnitsByItemCodeAsync(item.Code);
        await EnsureSubBomOptionsLoadedAsync();
        var line = CreateLineModel();
        line.SortOrder = Lines.Count + 1;
        line.MaterialCode = item.Code;
        line.MaterialName = item.Name;
        line.Unit = item.UnitCost;   // default unit code
        line.SelectedSubBom = BomSelectionOption.None;
        foreach (var u in units)
            line.AvailableUnits.Add(u);
        line.SelectedUnit = line.AvailableUnits.FirstOrDefault(u => u.Code == item.UnitCost);
        Lines.Add(line);
    }

    /// <summary>Remove the specified BOM line from the grid.</summary>
    [RelayCommand]
    private void RemoveLine(BomLineEditModel line)
    {
        Lines.Remove(line);
    }

    [RelayCommand]
    private void Cancel() => _navigation.NavigateTo<BomListViewModel>();

    [RelayCommand]
    private async Task SearchProductAsync()
    {
        if (ShowProductSearchDialog is null) return;
        var item = await ShowProductSearchDialog();
        if (item is not null)
            ItemCode = item.Code;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!ValidateForm())
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var lineCommands = Lines.Select(l => new CreateBomLineCommand(
                l.MaterialCode,
                l.Quantity,
                l.Unit,
                l.SubBomId,
                l.SortOrder,
                string.IsNullOrEmpty(l.Notes) ? null : l.Notes
            )).ToList();

            Result<BomDto> result;
            if (IsEditing)
            {
                var cmd = new UpdateBomCommand(
                    Name,
                    string.IsNullOrEmpty(Description) ? null : Description,
                    ItemCode,
                    YieldQuantity,
                    SelectedYieldUnit?.Code ?? YieldUnit,
                    lineCommands
                );
                result = await _bomService.UpdateAsync(EditingId!.Value, cmd);
            }
            else
            {
                var cmd = new CreateBomCommand(
                    Code,
                    Name,
                    string.IsNullOrEmpty(Description) ? null : Description,
                    ItemCode,
                    YieldQuantity,
                    SelectedYieldUnit?.Code ?? YieldUnit,
                    lineCommands
                );
                result = await _bomService.CreateAsync(cmd);
            }

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "บันทึกไม่สำเร็จ";
                return;
            }

            _navigation.NavigateTo<BomListViewModel>();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ------------------------------------------------------------------ //
    // Private helpers                                                      //
    // ------------------------------------------------------------------ //

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "กรุณากรอกรหัสสูตร";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "กรุณากรอกชื่อสูตร";
            return false;
        }
        if (string.IsNullOrWhiteSpace(ItemCode))
        {
            ErrorMessage = "กรุณาเลือกสินค้าที่ผลิต";
            return false;
        }
        if (YieldQuantity <= 0)
        {
            ErrorMessage = "จำนวนที่ผลิตได้ต้องมากกว่า 0";
            return false;
        }
        if (!Lines.Any())
        {
            ErrorMessage = "ต้องมีวัตถุดิบอย่างน้อย 1 รายการ";
            return false;
        }
        ErrorMessage = string.Empty;
        return true;
    }

    private BomLineEditModel CreateLineModel() => new(AvailableSubBoms);

    private async Task EnsureSubBomOptionsLoadedAsync()
    {
        if (AvailableSubBoms.Count <= 1)
            await LoadSubBomOptionsAsync();
    }

    private async Task LoadSubBomOptionsAsync()
    {
        var result = await _bomService.GetAllAsync();
        if (!result.IsSuccess || result.Value is null)
            return;

        AvailableSubBoms.Clear();
        AvailableSubBoms.Add(BomSelectionOption.None);

        foreach (var bom in result.Value
                     .Where(b => b.Id != EditingId)
                     .OrderBy(b => b.Code))
        {
            AvailableSubBoms.Add(new BomSelectionOption(
                bom.Id,
                $"{bom.Code} - {bom.Name} ({bom.Status})"));
        }

        foreach (var line in Lines)
            line.RefreshSelectedSubBom();
    }
}

public sealed record BomSelectionOption(Guid? Id, string DisplayName)
{
    public static BomSelectionOption None { get; } = new(null, "ไม่ใช้สูตรย่อย");
}

/// <summary>
/// Editable row model for BOM Line inline DataGrid.
/// Each instance represents one material line in the BOM editor.
/// </summary>
public partial class BomLineEditModel(ObservableCollection<BomSelectionOption> availableSubBoms) : ObservableObject
{
    [ObservableProperty] private string _materialCode = string.Empty;
    [ObservableProperty] private string _materialName = string.Empty;
    [ObservableProperty] private decimal _quantity = 1m;
    [ObservableProperty] private string _unit = string.Empty;
    [ObservableProperty] private Guid? _subBomId;
    [ObservableProperty] private int _sortOrder;
    [ObservableProperty] private string _notes = string.Empty;

    /// <summary>Units available for this material, fetched from ERP on row creation.</summary>
    public ObservableCollection<ErpUnitDto> AvailableUnits { get; } = new();

    public ObservableCollection<BomSelectionOption> AvailableSubBoms { get; } = availableSubBoms;

    /// <summary>
    /// The unit selected in the ComboBox. Setting this syncs <see cref="Unit"/>
    /// so SaveAsync sees the correct code without any extra wiring.
    /// </summary>
    [ObservableProperty] private ErpUnitDto? _selectedUnit;
    [ObservableProperty] private BomSelectionOption? _selectedSubBom = BomSelectionOption.None;

    partial void OnSelectedUnitChanged(ErpUnitDto? value)
    {
        if (value is not null)
            Unit = value.Code;
    }

    partial void OnSubBomIdChanged(Guid? value)
    {
        SelectedSubBom = AvailableSubBoms.FirstOrDefault(b => b.Id == value) ?? BomSelectionOption.None;
    }

    partial void OnSelectedSubBomChanged(BomSelectionOption? value)
    {
        SubBomId = value?.Id;
    }

    public void RefreshSelectedSubBom()
    {
        SelectedSubBom = AvailableSubBoms.FirstOrDefault(b => b.Id == SubBomId) ?? BomSelectionOption.None;
    }
}
