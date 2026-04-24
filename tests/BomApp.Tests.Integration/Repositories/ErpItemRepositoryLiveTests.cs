using BomApp.Infrastructure.Erp;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Tests.Integration.Repositories;

/// <summary>
/// Live tests against the real ERP database at 192.168.2.212/productbom.
/// ใช้สำหรับ diagnose ปัญหาเท่านั้น — ต้องการ network access ไปยัง server จริง.
/// Run ด้วย: dotnet test --filter "Category=Live"
/// </summary>
[Trait("Category", "Live")]
public class ErpItemRepositoryLiveTests
{
    private const string ConnStr =
        "Host=192.168.2.212;Port=5432;Database=productbom;Username=postgres;Password=sml;Timeout=10;SSL Mode=Prefer";

    private ErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseNpgsql(ConnStr)
            .Options;
        return new ErpDbContext(options);
    }

    [Fact]
    public async Task GetAllItemsAsync_LiveDb_ReturnsRows()
    {
        await using var ctx = CreateContext();
        var repo = new ErpItemRepository(ctx);

        var result = await repo.GetAllItemsAsync();

        // ถ้า ic_inventory มีข้อมูล ต้องได้ผลมากกว่า 0 rows
        result.Should().NotBeEmpty("ic_inventory ควรมีสินค้าอยู่ในฐานข้อมูล");
        result[0].Code.Should().NotBeNullOrEmpty();
        result[0].Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAllItemsAsync_LiveDb_CanConnect()
    {
        await using var ctx = CreateContext();

        // ทดสอบว่า connect ได้และ ic_inventory มีอยู่จริง
        var canConnect = await ctx.Database.CanConnectAsync();
        canConnect.Should().BeTrue("ต้อง connect ไปยัง productbom ได้");

        // ทดสอบ raw SQL เพื่อ check table ว่ามีอยู่ใน schema ใด
        var tableInfo = await ctx.Database
            .SqlQuery<TableInfoRaw>($"""
                SELECT table_schema AS Schema, table_name AS TableName
                FROM information_schema.tables
                WHERE table_name = 'ic_inventory'
                """)
            .ToListAsync();

        tableInfo.Should().NotBeEmpty(
            "ไม่พบ ic_inventory ในฐานข้อมูล productbom — ตรวจสอบ schema");

        // แสดง schema ที่พบ เพื่อ debug
        var found = tableInfo.Select(t => $"{t.Schema}.{t.TableName}");
        found.Should().NotBeEmpty($"พบ ic_inventory ใน: {string.Join(", ", found)}");
    }

    private sealed class TableInfoRaw
    {
        public string Schema { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
    }
}
