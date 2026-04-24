using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Shared.Contracts;

namespace BomApp.UI.ViewModels.Bom;

public partial class ProductSearchDialogViewModel : ObservableObject
{
    private readonly Func<string, Task<IReadOnlyList<ErpItemDto>>> _searchFunc;
    private IReadOnlyList<ErpItemDto> _allItems = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ErpItemDto? _selectedItem;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public IEnumerable<ErpItemDto> FilteredItems => string.IsNullOrWhiteSpace(SearchText)
        ? _allItems
        : _allItems.Where(i =>
            i.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    public ProductSearchDialogViewModel(Func<string, Task<IReadOnlyList<ErpItemDto>>> searchFunc)
    {
        _searchFunc = searchFunc;
    }

    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredItems));

    [RelayCommand]
    private async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            _allItems = await _searchFunc(string.Empty);
            OnPropertyChanged(nameof(FilteredItems));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
