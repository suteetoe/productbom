using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BomApp.Shared.Contracts;
using BomApp.UI.ViewModels.Bom;
using BomApp.UI.ViewModels.ProductDestruction;
using BomApp.UI.Views.Bom;

namespace BomApp.UI.Views.ProductDestruction;

public partial class ProductDestructionView : UserControl
{
    public ProductDestructionView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireCallbacks();
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ProductDestructionViewModel vm)
            await vm.LoadInitialCommand.ExecuteAsync(null);
    }

    private void WireCallbacks()
    {
        if (DataContext is not ProductDestructionViewModel vm)
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

        vm.ShowPicturePicker = async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return null;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "เลือกรูปภาพของเสีย",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Images")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"]
                    }
                ]
            });

            var file = files.FirstOrDefault();
            if (file is null)
                return null;

            await using var stream = await file.OpenReadAsync();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);

            return new ProductDestructionPictureEditModel
            {
                ImageGuid = Guid.NewGuid().ToString("N"),
                ImageFile = memory.ToArray()
            };
        };
    }
}
