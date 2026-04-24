using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BomApp.Shared.Contracts;
using BomApp.UI.ViewModels.Bom;

namespace BomApp.UI.Views.Bom;

public partial class ProductSearchDialog : Window
{
    public ProductSearchDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => CommitSelected();

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);

    private void OnRowDoubleTapped(object? sender, TappedEventArgs e) => CommitSelected();

    private void CommitSelected()
    {
        if (DataContext is ProductSearchDialogViewModel { SelectedItem: { } item })
            Close(item);
    }
}
