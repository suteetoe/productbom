using Avalonia.Controls.ApplicationLifetimes;
using BomApp.UI.Views.Bom;
using AvaloniaApp = Avalonia.Application;

namespace BomApp.UI.Services;

public sealed class DialogService : IDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var mainWindow = (AvaloniaApp.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow is null) return false;
        var dialog = new ConfirmDialog(title, message);
        return await dialog.ShowDialog<bool>(mainWindow);
    }

    public async Task AlertAsync(string title, string message)
    {
        var mainWindow = (AvaloniaApp.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow is null) return;
        var dialog = new MessageDialog(title, message);
        await dialog.ShowDialog(mainWindow);
    }
}
