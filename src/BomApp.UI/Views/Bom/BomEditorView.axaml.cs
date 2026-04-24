using Avalonia.Controls;
using Avalonia.Input;
using BomApp.Shared.Contracts;
using BomApp.UI.ViewModels.Bom;

namespace BomApp.UI.Views.Bom;

public partial class BomEditorView : UserControl
{
    public BomEditorView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireDialogCallback();
    }

    private void WireDialogCallback()
    {
        if (DataContext is not BomEditorViewModel vm) return;

        vm.ShowProductSearchDialog = async () =>
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window is null) return null;

            var dlgVm = new ProductSearchDialogViewModel(vm.SearchItemsAsync);
            await dlgVm.LoadCommand.ExecuteAsync(null);
            var dialog = new ProductSearchDialog { DataContext = dlgVm };
            return await dialog.ShowDialog<ErpItemDto?>(window);
        };

        // GotFocus: switch TextBox to code-only so the user can edit the code directly.
        ItemCodeBox.GotFocus += (_, _) =>
        {
            if (DataContext is not BomEditorViewModel boxVm) return;
            ItemCodeBox.SetCurrentValue(TextBox.TextProperty, boxVm.GetItemCodeOnly());
            ItemCodeBox.SelectAll();
        };

        // Enter key or LostFocus: commit the typed text as a product code, trigger lookup, refresh to "CODE~Name".
        async Task CommitAsync()
        {
            if (DataContext is not BomEditorViewModel boxVm) return;
            var rawText = ItemCodeBox.Text ?? string.Empty;
            await boxVm.CommitItemSearchTextAsync(rawText);
        }

        ItemCodeBox.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Return) return;
            await CommitAsync();
        };

        ItemCodeBox.LostFocus += async (_, _) => await CommitAsync();
    }
}
