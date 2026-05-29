namespace BomApp.Application.Configuration;

public sealed class DatabaseConnectionSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5432;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AuthDatabaseName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
