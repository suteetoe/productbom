using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.UI.Services;
using BomApp.UI.ViewModels.Bom;
using BomApp.UI.ViewModels.BomAssignment;
using BomApp.UI.ViewModels.Production;
using BomApp.UI.ViewModels.ProductManufacturing;
using BomApp.UI.ViewModels.ProductDestruction;
using BomApp.UI.ViewModels.SalesCalculation;

namespace BomApp.UI.ViewModels.Shell;

/// <summary>
/// Represents a single entry in the sidebar navigation list.
/// </summary>
public sealed class NavItemViewModel : ObservableObject
{
    public string Label { get; init; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

/// <summary>
/// ViewModel for the left-hand navigation sidebar.
///
/// Exposes 4 nav items matching the screens owned by Team B.
/// NavigateCommand switches the active item and calls INavigationService.
/// </summary>
public partial class SidebarViewModel : ViewModelBase
{
    // ------------------------------------------------------------------ //
    // Dependencies                                                         //
    // ------------------------------------------------------------------ //

    private readonly INavigationService _navigation;

    // ------------------------------------------------------------------ //
    // Observable Properties                                                //
    // ------------------------------------------------------------------ //

    [ObservableProperty]
    private NavItemViewModel? _selectedItem;

    public IReadOnlyList<NavItemViewModel> NavItems { get; }

    // ------------------------------------------------------------------ //
    // Constructor                                                          //
    // ------------------------------------------------------------------ //

    public SidebarViewModel(INavigationService navigation)
    {
        _navigation = navigation;

        NavItems = new List<NavItemViewModel>
        {
            new() { Label = "สูตรการผลิต" },
            new() { Label = "กำหนดสูตร" },
            new() { Label = "รายการผลิต" },
            new() { Label = "ผลิตสินค้า" },
            new() { Label = "เบิกของเสีย" },
            new() { Label = "คำนวณการผลิต" },
        };
    }

    // ------------------------------------------------------------------ //
    // Commands                                                             //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Navigate to the screen that corresponds to the tapped nav item.
    /// </summary>
    [RelayCommand]
    private void Navigate(NavItemViewModel item)
    {
        // Deselect all, then select the tapped item
        foreach (var nav in NavItems)
            nav.IsSelected = false;

        item.IsSelected = true;
        SelectedItem = item;

        // Map label to ViewModel type
        switch (item.Label)
        {
            case "สูตรการผลิต":
                _navigation.NavigateTo<BomListViewModel>();
                break;
            case "กำหนดสูตร":
                _navigation.NavigateTo<BomAssignmentViewModel>();
                break;
            case "รายการผลิต":
                _navigation.NavigateTo<ProductionListViewModel>();
                break;
            case "ผลิตสินค้า":
                _navigation.NavigateTo<ProductManufacturingViewModel>();
                break;
            case "เบิกของเสีย":
                _navigation.NavigateTo<ProductDestructionViewModel>();
                break;
            case "คำนวณการผลิต":
                _navigation.NavigateTo<SalesCalculationViewModel>();
                break;
        }
    }

    /// <summary>
    /// Called by MainWindowViewModel after a programmatic navigation
    /// (e.g. after login) so the sidebar reflects the correct active item.
    /// </summary>
    public void SetActiveByLabel(string label)
    {
        foreach (var nav in NavItems)
            nav.IsSelected = nav.Label == label;

        SelectedItem = NavItems.FirstOrDefault(n => n.Label == label);
    }
}
