using BomApp.UI.ViewModels;

namespace BomApp.UI.Services;

/// <summary>
/// Contract for application-level navigation.
/// ViewModels call NavigateTo to switch the current view without
/// holding any reference to a concrete View type.
/// </summary>
public interface INavigationService
{
    /// <summary>The ViewModel that is currently active in the main content area.</summary>
    ViewModelBase? CurrentViewModel { get; }

    /// <summary>Fired after every navigation. The argument is the newly active ViewModel.</summary>
    event EventHandler<ViewModelBase>? Navigated;

    /// <summary>
    /// Switch the active content area to the ViewModel identified by TViewModel.
    /// The service resolves the instance from its internal factory registry.
    /// </summary>
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;

    /// <summary>
    /// Same as NavigateTo but runs <paramref name="configure"/> on the fresh instance
    /// before raising Navigated — lets callers pass parameters to the ViewModel.
    /// </summary>
    void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : ViewModelBase;
}
