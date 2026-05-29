namespace BomApp.Application.Configuration;

public sealed class RuntimeAppSettings
{
    public DatabaseConnectionSettings DatabaseConnection { get; set; } = new();
    public string ErpWebServiceUrl { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
}
