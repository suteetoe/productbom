using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BomApp.Application.Configuration;
using BomApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BomApp.Infrastructure.Configuration;

public sealed class RuntimeConfigurationService : IRuntimeConfigurationService
{
    private const string AuthenticationDatabaseConnectionName = "authentication-database";
    private const string ErpDatabaseConnectionName = "erp-database";
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private bool _hasRuntimeSettings;

    public RuntimeConfigurationService(IConfiguration configuration)
        : this(configuration, GetDefaultConfigFilePath())
    {
    }

    public RuntimeConfigurationService(IConfiguration configuration, string configFilePath)
    {
        _configuration = configuration;
        ConfigFilePath = configFilePath;
        Current = CreateSettingsFromConnectionString(
            _configuration.GetConnectionString(ErpDatabaseConnectionName) ?? string.Empty,
            _configuration.GetConnectionString(AuthenticationDatabaseConnectionName) ?? string.Empty,
            _configuration["ErpWebServiceUrl"] ?? string.Empty,
            _configuration["ProviderCode"] ?? string.Empty);
    }

    public RuntimeAppSettings Current { get; private set; }
    public string ConfigFilePath { get; }

    public static string GetDefaultConfigFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
            appData = AppContext.BaseDirectory;

        return Path.Combine(appData, "BomApp", "bomapp.settings.json");
    }

    public RuntimeAppSettings Load()
    {
        if (!File.Exists(ConfigFilePath))
        {
            _hasRuntimeSettings = false;
            return Current;
        }

        var json = File.ReadAllText(ConfigFilePath);
        var fileSettings = JsonSerializer.Deserialize<RuntimeAppSettingsFile>(json, _jsonOptions);
        if (fileSettings is null)
        {
            _hasRuntimeSettings = false;
            return Current;
        }

        Current = new RuntimeAppSettings
        {
            DatabaseConnection = new DatabaseConnectionSettings
            {
                Host = fileSettings.DatabaseConnection.Host,
                Port = fileSettings.DatabaseConnection.Port,
                Username = fileSettings.DatabaseConnection.Username,
                Password = DecodePassword(fileSettings.DatabaseConnection.PasswordBase64),
                AuthDatabaseName = string.IsNullOrWhiteSpace(fileSettings.DatabaseConnection.AuthDatabaseName)
                    ? fileSettings.DatabaseConnection.DatabaseName
                    : fileSettings.DatabaseConnection.AuthDatabaseName,
                DatabaseName = fileSettings.DatabaseConnection.DatabaseName,
            },
            ErpWebServiceUrl = fileSettings.ErpWebServiceUrl,
            ProviderCode = fileSettings.ProviderCode,
        };
        _hasRuntimeSettings = true;
        return Current;
    }

    public async Task SaveAsync(RuntimeAppSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath) ?? AppContext.BaseDirectory);

        var fileSettings = new RuntimeAppSettingsFile
        {
            DatabaseConnection = new DatabaseConnectionSettingsFile
            {
                Host = settings.DatabaseConnection.Host.Trim(),
                Port = settings.DatabaseConnection.Port,
                Username = settings.DatabaseConnection.Username.Trim(),
                PasswordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.DatabaseConnection.Password)),
                AuthDatabaseName = settings.DatabaseConnection.AuthDatabaseName.Trim(),
                DatabaseName = settings.DatabaseConnection.DatabaseName.Trim(),
            },
            ErpWebServiceUrl = settings.ErpWebServiceUrl.Trim(),
            ProviderCode = settings.ProviderCode.Trim(),
        };

        await using (var stream = File.Create(ConfigFilePath))
        {
            await JsonSerializer.SerializeAsync(stream, fileSettings, _jsonOptions, cancellationToken);
        }

        Load();
    }

    public string GetConnectionString(string connectionName)
    {
        if (!_hasRuntimeSettings)
            return _configuration.GetConnectionString(connectionName) ?? string.Empty;

        var databaseName = connectionName == AuthenticationDatabaseConnectionName
            ? Current.DatabaseConnection.AuthDatabaseName
            : Current.DatabaseConnection.DatabaseName;

        return BuildConnectionString(Current.DatabaseConnection, databaseName);
    }

    private static RuntimeAppSettings CreateSettingsFromConnectionString(
        string erpConnectionString,
        string authenticationConnectionString,
        string erpWebServiceUrl,
        string providerCode)
    {
        var settings = new RuntimeAppSettings
        {
            ErpWebServiceUrl = erpWebServiceUrl,
            ProviderCode = providerCode,
        };

        if (string.IsNullOrWhiteSpace(erpConnectionString))
            return settings;

        var builder = new NpgsqlConnectionStringBuilder(erpConnectionString);
        settings.DatabaseConnection = new DatabaseConnectionSettings
        {
            Host = builder.Host ?? string.Empty,
            Port = builder.Port,
            Username = builder.Username ?? string.Empty,
            Password = builder.Password ?? string.Empty,
            DatabaseName = builder.Database ?? string.Empty,
        };

        if (!string.IsNullOrWhiteSpace(authenticationConnectionString))
        {
            var authBuilder = new NpgsqlConnectionStringBuilder(authenticationConnectionString);
            settings.DatabaseConnection.AuthDatabaseName = authBuilder.Database ?? string.Empty;
        }

        return settings;
    }

    private static string BuildConnectionString(DatabaseConnectionSettings settings, string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port,
            Username = settings.Username,
            Password = settings.Password,
            Database = databaseName,
            Timeout = 10,
            SslMode = SslMode.Prefer,
        };

        return builder.ConnectionString;
    }

    private static string DecodePassword(string passwordBase64)
    {
        if (string.IsNullOrWhiteSpace(passwordBase64))
            return string.Empty;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(passwordBase64));
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    private sealed class RuntimeAppSettingsFile
    {
        [JsonPropertyName("databaseConnection")]
        public DatabaseConnectionSettingsFile DatabaseConnection { get; set; } = new();

        [JsonPropertyName("erpWebServiceUrl")]
        public string ErpWebServiceUrl { get; set; } = string.Empty;

        [JsonPropertyName("providerCode")]
        public string ProviderCode { get; set; } = string.Empty;
    }

    private sealed class DatabaseConnectionSettingsFile
    {
        [JsonPropertyName("host")]
        public string Host { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public int Port { get; set; } = 5432;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("passwordBase64")]
        public string PasswordBase64 { get; set; } = string.Empty;

        [JsonPropertyName("authDatabaseName")]
        public string AuthDatabaseName { get; set; } = string.Empty;

        [JsonPropertyName("databaseName")]
        public string DatabaseName { get; set; } = string.Empty;
    }
}
