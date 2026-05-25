namespace BomApp.UI.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message);
    Task AlertAsync(string title, string message);
}
