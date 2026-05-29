using BomApp.Application.Configuration;
using BomApp.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BomApp.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IRuntimeConfigurationService _configurationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccessMessage))]
    private string _successMessage = string.Empty;

    [ObservableProperty]
    private string _host = string.Empty;

    [ObservableProperty]
    private string _port = "5432";

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _authDatabaseName = string.Empty;

    [ObservableProperty]
    private string _databaseName = string.Empty;

    [ObservableProperty]
    private string _erpWebServiceUrl = string.Empty;

    [ObservableProperty]
    private string _providerCode = string.Empty;

    [ObservableProperty]
    private bool _isSaving;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);

    public event EventHandler? CloseRequested;

    public SettingsViewModel(IRuntimeConfigurationService configurationService)
    {
        _configurationService = configurationService;
        LoadFromCurrentSettings();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (!TryBuildSettings(out var settings))
            return;

        IsSaving = true;
        try
        {
            await _configurationService.SaveAsync(settings);
            SuccessMessage = "บันทึกการตั้งค่าและ reload config database แล้ว";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ไม่สามารถบันทึกการตั้งค่าได้: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void LoadFromCurrentSettings()
    {
        var settings = _configurationService.Current;
        Host = settings.DatabaseConnection.Host;
        Port = settings.DatabaseConnection.Port.ToString();
        Username = settings.DatabaseConnection.Username;
        Password = settings.DatabaseConnection.Password;
        AuthDatabaseName = settings.DatabaseConnection.AuthDatabaseName;
        DatabaseName = settings.DatabaseConnection.DatabaseName;
        ErpWebServiceUrl = settings.ErpWebServiceUrl;
        ProviderCode = settings.ProviderCode;
    }

    private bool TryBuildSettings(out RuntimeAppSettings settings)
    {
        settings = new RuntimeAppSettings();

        if (string.IsNullOrWhiteSpace(Host))
        {
            ErrorMessage = "กรุณากรอก host";
            return false;
        }

        if (!int.TryParse(Port, out var port) || port is <= 0 or > 65535)
        {
            ErrorMessage = "กรุณากรอก port เป็นตัวเลข 1-65535";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "กรุณากรอก username";
            return false;
        }

        if (string.IsNullOrWhiteSpace(AuthDatabaseName))
        {
            ErrorMessage = "กรุณากรอก authen database name";
            return false;
        }

        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            ErrorMessage = "กรุณากรอก databaseName";
            return false;
        }

        settings = new RuntimeAppSettings
        {
            DatabaseConnection = new DatabaseConnectionSettings
            {
                Host = Host.Trim(),
                Port = port,
                Username = Username.Trim(),
                Password = Password,
                AuthDatabaseName = AuthDatabaseName.Trim(),
                DatabaseName = DatabaseName.Trim(),
            },
            ErpWebServiceUrl = ErpWebServiceUrl.Trim(),
            ProviderCode = ProviderCode.Trim(),
        };
        return true;
    }
}
