namespace BomApp.Application.Interfaces;

public interface IBomDatabaseInitializer
{
    Task EnsureReadyAsync(CancellationToken ct = default);
}
