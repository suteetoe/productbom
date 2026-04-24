using BomApp.UI.ViewModels;

namespace BomApp.UI.Services;

/// <summary>
/// Default NavigationService implementation.
///
/// A dictionary of ViewModel factories is registered at startup (via the
/// Register method).  Calling NavigateTo looks up the factory, instantiates
/// the ViewModel, stores it as CurrentViewModel, and fires the Navigated event
/// so that MainWindowViewModel can update its CurrentView property.
/// </summary>
public sealed class NavigationService : INavigationService
{
    // ------------------------------------------------------------------ //
    // Fields                                                               //
    // ------------------------------------------------------------------ //

    private readonly Dictionary<Type, Func<ViewModelBase>> _factories = new();

    // ------------------------------------------------------------------ //
    // INavigationService                                                   //
    // ------------------------------------------------------------------ //

    /// <inheritdoc/>
    public ViewModelBase? CurrentViewModel { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<ViewModelBase>? Navigated;

    /// <inheritdoc/>
    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var key = typeof(TViewModel);

        if (!_factories.TryGetValue(key, out var factory))
            throw new InvalidOperationException(
                $"No factory registered for ViewModel type '{key.FullName}'. " +
                "Call NavigationService.Register before navigating.");

        var viewModel = factory();
        CurrentViewModel = viewModel;
        Navigated?.Invoke(this, viewModel);
    }

    /// <inheritdoc/>
    public void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : ViewModelBase
    {
        var key = typeof(TViewModel);

        if (!_factories.TryGetValue(key, out var factory))
            throw new InvalidOperationException(
                $"No factory registered for ViewModel type '{key.FullName}'. " +
                "Call NavigationService.Register before navigating.");

        var viewModel = (TViewModel)factory();
        configure(viewModel);
        CurrentViewModel = viewModel;
        Navigated?.Invoke(this, viewModel);
    }

    // ------------------------------------------------------------------ //
    // Registration API (called from App startup / DI composition root)    //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Register a factory that produces a ViewModel of type TViewModel on demand.
    /// Each call to NavigateTo creates a fresh instance via the factory.
    /// </summary>
    public void Register<TViewModel>(Func<TViewModel> factory)
        where TViewModel : ViewModelBase
    {
        _factories[typeof(TViewModel)] = () => factory();
    }
}
