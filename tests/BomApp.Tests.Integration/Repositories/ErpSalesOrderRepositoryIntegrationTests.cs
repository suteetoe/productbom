using BomApp.Infrastructure.Erp;
using BomApp.Shared.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Tests.Integration.Repositories;

public class ErpSalesOrderRepositoryIntegrationTests : ErpDbIntegrationTestBase
{
    protected override async Task SeedAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_trans_detail (
                doc_date,
                doc_no,
                trans_flag,
                last_status,
                item_code,
                qty,
                unit_code,
                stand_value,
                divide_value,
                wh_code,
                shelf_code
            )
            VALUES (
                DATE '2024-01-15',
                'SO-2024-0001',
                44,
                0,
                'PROD-001',
                10,
                'PCS',
                1,
                1,
                'WH-A',
                'SH-01'
            )
            """);
    }

    [Fact]
    public async Task GetSalesTransactionsByDateRangeAsync_ReturnsWarehouseAndShelf()
    {
        var repo = new ErpSalesOrderRepository(DbContext);

        var result = await repo.GetSalesTransactionsByDateRangeAsync(
            new DateOnly(2024, 1, 15),
            new DateOnly(2024, 1, 15));

        result.Should().ContainSingle().Which.Should().Match<ErpSalesTransactionDto>(t =>
            t.DocNo == "SO-2024-0001" &&
            t.WhCode == "WH-A" &&
            t.ShelfCode == "SH-01");
    }
}
