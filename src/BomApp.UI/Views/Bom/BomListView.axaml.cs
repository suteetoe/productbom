using Avalonia.Controls;
using BomApp.UI.ViewModels.Bom;

namespace BomApp.UI.Views.Bom;

public partial class BomListView : UserControl
{
    public BomListView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is BomListViewModel vm)
                vm.LoadCommand.Execute(null);
        };
    }
}
