using Avalonia.Controls;
using BomApp.UI.ViewModels.BomAssignment;

namespace BomApp.UI.Views.BomAssignment;

public partial class BomAssignmentView : UserControl
{
    public BomAssignmentView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is BomAssignmentViewModel vm)
                vm.LoadCommand.Execute(null);
        };
    }
}
