using CommunityToolkit.Mvvm.ComponentModel;
using BomApp.UI.Services;
using BomApp.UI.ViewModels.Shell;

namespace BomApp.UI.ViewModels;

/// <summary>
/// Root ViewModel for the application shell (MainWindow).
///
/// Owns two things:
///   1. Sidebar    — left navigation panel
///   2. CurrentView — the active content ViewModel shown in the right panel
///
/// Subscribes to INavigationService.Navigated so that any ViewModel can
/// trigger navigation without holding a reference to MainWindowViewModel.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    // ------------------------------------------------------------------ //
    // Dependencies                                                         //
    // ------------------------------------------------------------------ //

    private readonly INavigationService _navigation;

    // ------------------------------------------------------------------ //
    // Observable Properties                                                //
    // ------------------------------------------------------------------ //

    /// <summary>ViewModel displayed inside the main ContentControl (right panel).</summary>
    [ObservableProperty]
    private ViewModelBase? _currentView;

    /// <summary>Left-hand sidebar navigation.</summary>
    public SidebarViewModel Sidebar { get; }

    // ------------------------------------------------------------------ //
    // Constructor                                                          //
    // ------------------------------------------------------------------ //

    public MainWindowViewModel(INavigationService navigation, SidebarViewModel sidebar)
    {
        _navigation = navigation;
        Sidebar = sidebar;

        // Keep CurrentView in sync whenever any ViewModel triggers navigation
        _navigation.Navigated += OnNavigated;
    }

    // ------------------------------------------------------------------ //
    // Event Handlers                                                       //
    // ------------------------------------------------------------------ //

    private void OnNavigated(object? sender, ViewModelBase viewModel)
    {
        CurrentView = viewModel;

        // Reflect the navigation in the sidebar highlight.
        // Map ViewModel type back to sidebar label.
        var label = viewModel.GetType().Name switch
        {
            "BomListViewModel"          => "สูตรการผลิต",
            "BomAssignmentViewModel"    => "กำหนดสูตร",
            "ProductionListViewModel"   => "รายการผลิต",
            "SalesCalculationViewModel" => "คำนวณการผลิต",
            _                           => string.Empty,
        };

        if (!string.IsNullOrEmpty(label))
            Sidebar.SetActiveByLabel(label);
    }
}
