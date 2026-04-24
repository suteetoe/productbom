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
            services.AddApplicationServices();
            services.AddInfrastructureServices(config);
            var sp = services.BuildServiceProvider();

            RegisterViewModels(NavigationService, sp);

            var authRepo    = sp.GetRequiredService<IAuthRepository>();
            var loginVm     = new LoginViewModel(NavigationService, authRepo);
            var loginWindow = new LoginView { DataContext = loginVm };

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

        nav.Register<BomListViewModel>(() =>
            new BomListViewModel(sp.GetRequiredService<IBomService>(), nav, dialogService));

        nav.Register<BomEditorViewModel>(() =>
            new BomEditorViewModel(
                sp.GetRequiredService<IBomService>(),
                nav,
                sp.GetRequiredService<IErpItemRepository>()));

        nav.Register<BomAssignmentViewModel>(() =>
            new BomAssignmentViewModel(
                sp.GetRequiredService<IBomAssignmentService>(),
                sp.GetRequiredService<IErpItemRepository>()));

        nav.Register<ProductionListViewModel>(() =>
            new ProductionListViewModel(sp.GetRequiredService<IProductionService>()));

        nav.Register<SalesCalculationViewModel>(() =>
            new SalesCalculationViewModel(
                sp.GetRequiredService<ICalculateSalesProductionUseCase>(),
                sp.GetRequiredService<IErpSalesOrderRepository>()));
    }
}
