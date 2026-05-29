using BomApp.Application.Configuration;

namespace BomApp.Application.Interfaces;

public interface IRuntimeConfigurationService
{
    RuntimeAppSettings Current { get; }
    string ConfigFilePath { get; }

    RuntimeAppSettings Load();
    Task SaveAsync(RuntimeAppSettings settings, CancellationToken cancellationToken = default);
    string GetConnectionString(string connectionName);
}
