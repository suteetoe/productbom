using Avalonia.Controls;
using BomApp.Shared.Contracts;
using BomApp.UI.ViewModels.Bom;
using BomApp.UI.ViewModels.ProductManufacturing;
using BomApp.UI.Views.Bom;

namespace BomApp.UI.Views.ProductManufacturing;

public partial class ProductManufacturingView : UserControl
{
    public ProductManufacturingView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireCallbacks();
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ProductManufacturingViewModel vm)
            await vm.LoadInitialCommand.ExecuteAsync(null);
    }

    private void WireCallbacks()
    {
        if (DataContext is not ProductManufacturingViewModel vm)
            return;

        vm.ShowProductSearchDialog = async () =>
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window is null)
                return null;

            var dlgVm = new ProductSearchDialogViewModel(vm.SearchItemsAsync);
            await dlgVm.LoadCommand.ExecuteAsync(null);
            var dialog = new ProductSearchDialog { DataContext = dlgVm };
            return await dialog.ShowDialog<ErpItemDto?>(window);
        };
    }
}
