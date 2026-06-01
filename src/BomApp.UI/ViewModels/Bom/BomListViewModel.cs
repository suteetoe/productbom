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
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _totalPages = 1;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasItems => Items.Count > 0;

    public bool CanGoPrevious => PageNumber > 1 && !IsLoading;

    public bool CanGoNext => PageNumber < TotalPages && !IsLoading;

    public string PageSummary => $"หน้า {PageNumber} / {TotalPages} ({TotalCount} รายการ)";

    public ObservableCollection<BomDto> Items { get; } = new();

    public IReadOnlyList<int> PageSizeOptions { get; } = [10, 20, 50, 100];

    /// <summary>
    /// Current page rows. Search is handled by the repository so the total count stays correct.
    /// </summary>
    public IEnumerable<BomDto> FilteredItems => Items;

    public BomListViewModel(IBomService bomService, INavigationService navigation, IDialogService dialogService)
    {
        _bomService = bomService;
        _navigation = navigation;
        _dialogService = dialogService;
    }

    /// <summary>Search starts again from the first page.</summary>
    partial void OnSearchTextChanged(string value)
    {
        PageNumber = 1;
        _ = LoadAsync();
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
        _ = LoadAsync();
    }

    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(PageSummary));
    }

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageSummary));
    }

    // ------------------------------------------------------------------ //
    // Commands                                                             //
    // ------------------------------------------------------------------ //

    /// <summary>Load current BOM page from service. Called on View activation.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _bomService.GetPageAsync(new BomListQuery(SearchText, PageNumber, PageSize));
            Items.Clear();
            if (result.IsSuccess)
            {
                var page = result.Value!;
                TotalCount = page.TotalCount;
                TotalPages = page.TotalPages;
                PageNumber = Math.Min(page.PageNumber, TotalPages);
                PageSize = page.PageSize;

                foreach (var bom in page.Items)
                    Items.Add(bom);
                OnPropertyChanged(nameof(FilteredItems));
                OnPropertyChanged(nameof(HasItems));
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

    /// <summary>Delete the given BOM after showing a confirmation dialog.</summary>
    [RelayCommand]
    private async Task DeleteAsync(BomDto bom)
    {
        var confirmed = await _dialogService.ConfirmAsync(
            "ยืนยันการลบ",
            $"ต้องการลบสูตร '{bom.Name}' ใช่หรือไม่?");
        if (!confirmed) return;

        var result = await _bomService.DeleteAsync(bom.Id);
        if (result.IsSuccess)
        {
            Items.Remove(bom);
            OnPropertyChanged(nameof(FilteredItems));
            OnPropertyChanged(nameof(HasItems));
            TotalCount = Math.Max(0, TotalCount - 1);
            if (Items.Count == 0 && PageNumber > 1)
                PageNumber--;
            await LoadAsync();
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
