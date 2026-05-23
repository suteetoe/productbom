using BomApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BomApp.Tests.Integration;

/// <summary>
/// Base class สำหรับ integration tests ที่ต้องการ PostgreSQL จริง.
/// ใช้ Testcontainers เพื่อ spin up PostgreSQL container per test class.
/// </summary>
public abstract class BomDbIntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("bom_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected BomDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseNpgsql(
                _postgres.GetConnectionString(),
                o => o.MigrationsHistoryTable("__EFMigrationsHistory", "public"))
            .Options;

        DbContext = new BomDbContext(options);

        // Run EF migrations to create schema + tables
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _postgres.StopAsync();
        await _postgres.DisposeAsync();
    }
}
