using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Shared.Contracts;

namespace BomApp.UI.ViewModels.Bom;

/// <summary>
/// ViewModel สำหรับหน้า BOM Editor (system-spec 2.2b).
/// Sprint 2: form fields + BOM lines collection พร้อม; service wiring ใน Sprint 3.
///
/// Bindings สำคัญ:
///   Code, Name, Description → TextBox.Text
///   ItemCode, ItemName       → lookup TextBox fields
///   YieldQuantity            → NumericUpDown หรือ TextBox
///   YieldUnit                → ComboBox.SelectedItem
///   Version                  → TextBlock (read-only badge)
///   Status                   → TextBlock (read-only badge)
///   IsEditing                → View title toggle ("สร้าง" vs "แก้ไข")
///   Lines                    → DataGrid.ItemsSource (inline edit)
///   IsLoading                → ProgressBar.IsVisible
///   HasError                 → error TextBlock.IsVisible
///   ErrorMessage             → error TextBlock.Text
///   AddLineCommand           → ปุ่ม "+ เพิ่มวัตถุดิบ"
///   RemoveLineCommand        → ปุ่ม "ลบ" ใน BOM lines DataGrid row
///   SaveCommand              → ปุ่ม "บันทึก"
/// </summary>
public partial class BomEditorViewModel : ViewModelBase
{
    private readonly IBomService _bomService;

    // ------------------------------------------------------------------ //
    // Header fields                                                        //
    // ------------------------------------------------------------------ //

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _itemCode = string.Empty;
    [ObservableProperty] private string _itemName = string.Empty;
    [ObservableProperty] private decimal _yieldQuantity = 1m;
    [ObservableProperty] private string _yieldUnit = string.Empty;
    [ObservableProperty] private string _status = "Draft";
    [ObservableProperty] private int _version = 1;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    // ------------------------------------------------------------------ //
    // Edit mode tracking                                                   //
    // ------------------------------------------------------------------ //

    /// <summary>null = creating new; non-null = editing existing BOM.</summary>
    public Guid? EditingId { get; init; }

    /// <summary>True when editing an existing BOM; false when creating new.</summary>
    public bool IsEditing => EditingId.HasValue;

    // ------------------------------------------------------------------ //
    // BOM Lines                                                            //
    // ------------------------------------------------------------------ //

    /// <summary>BOM line rows bound to the inline-editable DataGrid.</summary>
    public ObservableCollection<BomLineEditModel> Lines { get; } = new();

    // ------------------------------------------------------------------ //
    // Constructor                                                          //
    // ------------------------------------------------------------------ //

    public BomEditorViewModel(IBomService bomService)
    {
        _bomService = bomService;
    }

    // ------------------------------------------------------------------ //
    // Commands                                                             //
    // ------------------------------------------------------------------ //

    /// <summary>Append a new empty BOM line to the grid.</summary>
    [RelayCommand]
    private void AddLine()
    {
        Lines.Add(new BomLineEditModel { SortOrder = Lines.Count + 1 });
    }

    /// <summary>Remove the specified BOM line from the grid.</summary>
    [RelayCommand]
    private void RemoveLine(BomLineEditModel line)
    {
        Lines.Remove(line);
    }

    /// <summary>
    /// Save header + lines.
    /// Sprint 2: placeholder delay — real Create/Update wiring in Sprint 3.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!ValidateForm())
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            // TODO Sprint 3: call IBomService.CreateAsync or UpdateAsync depending on IsEditing
            await Task.Delay(100);
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
}

/// <summary>
/// Editable row model for BOM Line inline DataGrid.
/// Each instance represents one material line in the BOM editor.
/// </summary>
public partial class BomLineEditModel : ObservableObject
{
    [ObservableProperty] private string _materialCode = string.Empty;
    [ObservableProperty] private string _materialName = string.Empty;
    [ObservableProperty] private decimal _quantity = 1m;
    [ObservableProperty] private string _unit = string.Empty;
    [ObservableProperty] private Guid? _subBomId;
    [ObservableProperty] private int _sortOrder;
    [ObservableProperty] private string _notes = string.Empty;
}
