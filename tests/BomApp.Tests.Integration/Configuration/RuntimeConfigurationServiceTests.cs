using BomApp.Application.Configuration;
using BomApp.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace BomApp.Tests.Integration.Configuration;

public class RuntimeConfigurationServiceTests
{
    [Fact]
    public void Constructor_UsesApplicationDataFolderByDefault()
    {
        var configuration = new ConfigurationBuilder().Build();
        var service = new RuntimeConfigurationService(configuration);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        service.ConfigFilePath.Should().Be(Path.Combine(appData, "BomApp", "bomapp.settings.json"));
        service.ConfigFilePath.Should().NotStartWith(AppContext.BaseDirectory);
    }

    [Fact]
    public async Task SaveAsync_WritesPasswordAsBase64AndReloadsConnectionString()
    {
        var directory = Directory.CreateTempSubdirectory("bomapp-settings-");
        var settingsPath = Path.Combine(directory.FullName, "bomapp.settings.json");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:erp-database"] = "Host=old-host;Port=5432;Database=old-db;Username=old-user;Password=old-password",
                    ["ConnectionStrings:authentication-database"] = "Host=auth-host;Port=5432;Database=auth-db;Username=auth-user;Password=auth-password",
                })
                .Build();

            var service = new RuntimeConfigurationService(configuration, settingsPath);

            await service.SaveAsync(new RuntimeAppSettings
            {
                DatabaseConnection = new DatabaseConnectionSettings
                {
                    Host = "db.example.local",
                    Port = 15432,
                    Username = "postgres",
                    Password = "secret",
                    AuthDatabaseName = "smlerpmaindebug",
                    DatabaseName = "productbom",
                },
                ErpWebServiceUrl = "https://erp.example.local/ws",
                ProviderCode = "SML",
            });

            var json = await File.ReadAllTextAsync(settingsPath);

            json.Should().Contain("\"passwordBase64\": \"c2VjcmV0\"");
            json.Should().Contain("\"authDatabaseName\": \"smlerpmaindebug\"");
            json.Should().Contain("\"providerCode\": \"SML\"");
            json.Should().NotContain("\"secret\"");
            service.Current.DatabaseConnection.Password.Should().Be("secret");
            service.Current.DatabaseConnection.AuthDatabaseName.Should().Be("smlerpmaindebug");
            service.Current.ErpWebServiceUrl.Should().Be("https://erp.example.local/ws");
            service.Current.ProviderCode.Should().Be("SML");
            service.GetConnectionString("erp-database").Should().Contain("Host=db.example.local");
            service.GetConnectionString("erp-database").Should().Contain("Database=productbom");
            service.GetConnectionString("authentication-database").Should().Contain("Database=smlerpmaindebug");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
