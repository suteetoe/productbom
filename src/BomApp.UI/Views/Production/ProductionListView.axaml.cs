using Avalonia.Controls;
using BomApp.UI.ViewModels.Production;

namespace BomApp.UI.Views.Production;

/// <summary>Production List view — code-behind is intentionally minimal.</summary>
public partial class ProductionListView : UserControl
{
    public ProductionListView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ProductionListViewModel vm)
                vm.LoadInitialCommand.Execute(null);
        };
    }
}
