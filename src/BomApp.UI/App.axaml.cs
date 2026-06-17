using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BomApp.Application;
using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Infrastructure;
using BomApp.UI.Services;
using BomApp.UI.ViewModels;
using BomApp.UI.ViewModels.Bom;
using BomApp.UI.ViewModels.BomAssignment;
using BomApp.UI.ViewModels.Production;
using BomApp.UI.ViewModels.ProductDestruction;
using BomApp.UI.ViewModels.SalesCalculation;
using BomApp.UI.ViewModels.Shell;
using BomApp.UI.Views;
using BomApp.UI.Views.Shell;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BomApp.UI;

public partial class App : Avalonia.Application
{
    public static NavigationService NavigationService { get; } = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddApplicationServices();
            services.AddInfrastructureServices(config);
            var sp = services.BuildServiceProvider();

            RegisterViewModels(NavigationService, sp);

            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var configService = sp.GetRequiredService<IRuntimeConfigurationService>();
            var loginVm = new LoginViewModel(NavigationService, scopeFactory);
            var loginWindow = new LoginView { DataContext = loginVm };
            loginVm.SettingsRequested += (_, _) =>
            {
                var settingsVm = new SettingsViewModel(configService);
                var settingsWindow = new SettingsView { DataContext = settingsVm };
                settingsVm.CloseRequested += (_, _) => settingsWindow.Close();
                settingsWindow.ShowDialog(loginWindow);
            };

            EventHandler<ViewModelBase>? handler = null;
            handler = (_, firstVm) =>
            {
                NavigationService.Navigated -= handler;

                var sidebar = new SidebarViewModel(NavigationService);
                var mainVm  = new MainWindowViewModel(NavigationService, sidebar);

                mainVm.CurrentView = firstVm;

                var mainWindow = new MainWindow { DataContext = mainVm };
                desktop.MainWindow = mainWindow;
                mainWindow.Show();

                loginWindow.Close();
            };

            NavigationService.Navigated += handler;

            desktop.MainWindow = loginWindow;
            loginWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterViewModels(NavigationService nav, IServiceProvider sp)
    {
        var dialogService = new DialogService();
        var scopeFactory  = sp.GetRequiredService<IServiceScopeFactory>();

        // Each factory lambda creates a NEW DI scope so every ViewModel
        // instantiation gets its own BomDbContext (and sibling scoped services).
        // This prevents the change-tracker from accumulating stale entity state
        // across repeated navigations when DbContext is resolved from the root
        // scope (which would make it effectively a singleton).
        //
        // NOTE: The scope is intentionally not disposed here — it lives as long
        // as the ViewModel is alive.  In an Avalonia desktop app with a single
        // active VM per page this is acceptable; memory is reclaimed when the
        // next navigation replaces the VM reference.

        nav.Register<BomListViewModel>(() =>
        {
            var scope = scopeFactory.CreateScope();
            return new BomListViewModel(scope.ServiceProvider.GetRequiredService<IBomService>(), nav, dialogService);
        });

        nav.Register<BomEditorViewModel>(() =>
        {
            var scope = scopeFactory.CreateScope();
            return new BomEditorViewModel(
                scope.ServiceProvider.GetRequiredService<IBomService>(),
                nav,
                scope.ServiceProvider.GetRequiredService<IErpItemRepository>());
        });

        nav.Register<BomAssignmentViewModel>(() =>
        {
            var scope = scopeFactory.CreateScope();
            return new BomAssignmentViewModel(
                scope.ServiceProvider.GetRequiredService<IBomAssignmentService>(),
                scope.ServiceProvider.GetRequiredService<IErpItemRepository>());
        });

        nav.Register<ProductionListViewModel>(() =>
        {
            var scope = scopeFactory.CreateScope();
            return new ProductionListViewModel(
                scope.ServiceProvider.GetRequiredService<IProductionService>(),
                dialogService);
        });

        nav.Register<ProductDestructionViewModel>(() =>
        {
            var scope = scopeFactory.CreateScope();
            return new ProductDestructionViewModel(
                scope.ServiceProvider.GetRequiredService<IProductDestructionService>(),
                scope.ServiceProvider.GetRequiredService<IErpItemRepository>(),
                dialogService);
        });

        nav.Register<SalesCalculationViewModel>(() =>
        {
            var scope = scopeFactory.CreateScope();
            return new SalesCalculationViewModel(
                scope.ServiceProvider.GetRequiredService<ICalculateSalesProductionUseCase>(),
                scope.ServiceProvider.GetRequiredService<IErpSalesOrderRepository>(),
                dialogService);
        });
    }
}
