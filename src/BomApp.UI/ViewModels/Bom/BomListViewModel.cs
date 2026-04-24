using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Shared.Contracts;
using BomApp.UI.Services;

namespace BomApp.UI.ViewModels.Bom;

/// <summary>
/// ViewModel สำหรับหน้า BOM List (system-spec 2.2a).
///
/// Bindings สำคัญ:
///   FilteredItems  → DataGrid.ItemsSource
///   SearchText     → TextBox.Text (TwoWay)
///   IsLoading      → ProgressBar.IsVisible
///   HasError       → error TextBlock.IsVisible
///   ErrorMessage   → error TextBlock.Text
///   LoadCommand    → เรียกตอน View load
///   CreateNewCommand → ปุ่ม "+ สร้างสูตรใหม่"
///   EditCommand    → ปุ่ม "แก้ไข" ใน DataGrid row
///   DeleteCommand  → ปุ่ม "ลบ" ใน DataGrid row
///   ActivateCommand   → ปุ่ม "เปิดใช้งาน"
///   DeactivateCommand → ปุ่ม "ปิดใช้งาน"
/// </summary>
public partial class BomListViewModel : ViewModelBase
{
    private readonly IBomService _bomService;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<BomDto> Items { get; } = new();

    /// <summary>
    /// Filtered view — re-evaluated whenever SearchText changes.
    /// Filters on Code and Name (case-insensitive).
    /// </summary>
    public IEnumerable<BomDto> FilteredItems => string.IsNullOrWhiteSpace(SearchText)
        ? Items
        : Items.Where(b =>
            b.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            b.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    public BomListViewModel(IBomService bomService, INavigationService navigation)
    {
        _bomService = bomService;
        _navigation = navigation;
    }

    /// <summary>Re-evaluate FilteredItems whenever SearchText changes.</summary>
    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredItems));

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    // ------------------------------------------------------------------ //
    // Commands                                                             //
    // ------------------------------------------------------------------ //

    /// <summary>Load all BOMs from service. Called on View activation.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _bomService.GetAllAsync();
            Items.Clear();
            if (result.IsSuccess)
            {
                foreach (var bom in result.Value!)
                    Items.Add(bom);
                OnPropertyChanged(nameof(FilteredItems));
            }
            else
            {
                ErrorMessage = result.Error ?? "เกิดข้อผิดพลาดในการโหลดข้อมูล";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Navigate to BOM Editor in create-new mode.</summary>
    [RelayCommand]
    private void CreateNew()
    {
        _navigation.NavigateTo<BomEditorViewModel>();
    }

    /// <summary>Navigate to BOM Editor in edit mode for the given BOM.</summary>
    [RelayCommand]
    private void Edit(BomDto bom)
    {
        _navigation.NavigateTo<BomEditorViewModel>(vm => _ = vm.LoadForEditAsync(bom.Id));
    }

    /// <summary>Delete the given BOM after confirmation (confirmation dialog handled in View/dialog service).</summary>
    [RelayCommand]
    private async Task DeleteAsync(BomDto bom)
    {
        // Confirmation dialog is the responsibility of the View layer via a dialog service.
        // For Sprint 2, the command executes immediately. Sprint 3 will add IDialogService.
        var result = await _bomService.DeleteAsync(bom.Id);
        if (result.IsSuccess)
        {
            Items.Remove(bom);
            OnPropertyChanged(nameof(FilteredItems));
        }
        else
        {
            ErrorMessage = result.Error ?? "ลบไม่สำเร็จ";
        }
    }

    /// <summary>Activate the given BOM (Draft/Inactive → Active).</summary>
    [RelayCommand]
    private async Task ActivateAsync(BomDto bom)
    {
        var result = await _bomService.ActivateAsync(bom.Id);
        if (result.IsSuccess)
            await LoadAsync();
        else
            ErrorMessage = result.Error ?? "เปิดใช้งานไม่สำเร็จ";
    }

    /// <summary>Deactivate the given BOM (Active → Inactive).</summary>
    [RelayCommand]
    private async Task DeactivateAsync(BomDto bom)
    {
        var result = await _bomService.DeactivateAsync(bom.Id);
        if (result.IsSuccess)
            await LoadAsync();
        else
            ErrorMessage = result.Error ?? "ปิดใช้งานไม่สำเร็จ";
    }
}
