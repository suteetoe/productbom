using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces.Repositories;
using BomApp.UI.Services;
using BomApp.UI.ViewModels.Bom;

namespace BomApp.UI.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IAuthRepository _authRepo;

    // ------------------------------------------------------------------ //
    // Observable Properties                                                //
    // ------------------------------------------------------------------ //

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
    private bool _rememberMe;

    [ObservableProperty]
    private bool _isLoading;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public LoginViewModel(INavigationService navigation, IAuthRepository authRepo)
    {
        _navigation = navigation;
        _authRepo   = authRepo;
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
            var user = await _authRepo.ValidateUserAsync(Username, Password);
            if (user is null)
            {
                ErrorMessage = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง";
                return;
            }
            _navigation.NavigateTo<BomListViewModel>();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
