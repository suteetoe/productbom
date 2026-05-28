using BomApp.Infrastructure.Erp;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BomApp.Tests.Integration;

/// <summary>
/// Base class สำหรับ integration tests ที่ทดสอบ ERP repositories.
/// สร้าง ic_inventory, ic_unit_use ด้วย raw DDL เพราะ ErpDbContext ไม่มี migrations.
/// </summary>
public abstract class ErpDbIntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("erp_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected ErpDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        DbContext = new ErpDbContext(options);

        await DbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ic_inventory (
                code      VARCHAR(25)  NOT NULL,
                name_1    VARCHAR(255) NOT NULL,
                unit_cost VARCHAR(25)  NOT NULL DEFAULT ''
            )
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ic_unit_use (
                code          VARCHAR(25) NOT NULL,
                ic_code       VARCHAR(25) NOT NULL,
                name_1        VARCHAR(255) NOT NULL DEFAULT '',
                stand_value   NUMERIC(18,6) NOT NULL DEFAULT 1,
                divide_value  NUMERIC(18,6) NOT NULL DEFAULT 1,
                ratio         INT NOT NULL DEFAULT 1,
                line_number   INT NOT NULL DEFAULT 1
            )
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ic_unit (
                code   VARCHAR(25)  NOT NULL,
                name_1 VARCHAR(255) NOT NULL
            )
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ic_trans_detail (
                trans_type SMALLINT NOT NULL DEFAULT 0,
                doc_date DATE NOT NULL,
                doc_time VARCHAR(5) NOT NULL DEFAULT '00:00',
                doc_date_calc DATE NOT NULL DEFAULT DATE '1900-01-01',
                doc_time_calc VARCHAR(5) NOT NULL DEFAULT '00:00',
                calc_flag SMALLINT NOT NULL DEFAULT 0,
                doc_no VARCHAR(50) NOT NULL,
                trans_flag SMALLINT NOT NULL,
                last_status SMALLINT NOT NULL DEFAULT 0,
                item_code VARCHAR(50) NOT NULL,
                item_name VARCHAR(255) NOT NULL DEFAULT '',
                qty NUMERIC(18,6) NOT NULL,
                unit_code VARCHAR(50) NOT NULL,
                stand_value NUMERIC(18,6) NOT NULL DEFAULT 1,
                divide_value NUMERIC(18,6) NOT NULL DEFAULT 1,
                wh_code VARCHAR(50) NOT NULL DEFAULT '',
                shelf_code VARCHAR(50) NOT NULL DEFAULT '',
                line_number INT NOT NULL DEFAULT 0
            )
            """);

        await SeedAsync();
    }

    /// <summary>Override เพื่อ insert ข้อมูล seed สำหรับ test class นั้น</summary>
    protected virtual Task SeedAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _postgres.StopAsync();
        await _postgres.DisposeAsync();
    }
}
