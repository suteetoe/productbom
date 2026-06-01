using BomApp.Application.Interfaces.Repositories;
using BomApp.UI.Services;
using BomApp.UI.ViewModels.Bom;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BomApp.UI.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public event EventHandler? SettingsRequested;

    public LoginViewModel(INavigationService navigation, IServiceScopeFactory scopeFactory)
    {
        _navigation = navigation;
        _scopeFactory = scopeFactory;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "กรุณากรอกชื่อผู้ใช้";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "กรุณากรอกรหัสผ่าน";
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var authRepo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var user = await authRepo.ValidateUserAsync(Username, Password);
            if (user is null)
            {
                ErrorMessage = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง";
                return;
            }

            _navigation.NavigateTo<BomListViewModel>();
        }
        catch (Exception)
        {
            ErrorMessage = "ไม่สามารถเชื่อมต่อฐานข้อมูลได้ กรุณาตรวจสอบการเชื่อมต่อเครือข่าย";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand]
    private void OpenSettings()
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }
}
