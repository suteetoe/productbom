using BomApp.Infrastructure.Erp;
using BomApp.Shared.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Tests.Integration.Repositories;

public class ErpProductionRepositoryIntegrationTests : ErpDbIntegrationTestBase
{
    protected override async Task SeedAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ic_trans (
                trans_type SMALLINT NOT NULL,
                trans_flag SMALLINT NOT NULL,
                doc_date DATE NOT NULL,
                doc_time VARCHAR(5) NOT NULL,
                doc_no VARCHAR(30) NOT NULL
            )
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ic_trans_detail (
                trans_type SMALLINT NOT NULL,
                trans_flag SMALLINT NOT NULL,
                doc_date DATE NOT NULL,
                doc_time VARCHAR(5) NOT NULL,
                doc_date_calc DATE NOT NULL,
                doc_time_calc VARCHAR(5) NOT NULL,
                calc_flag SMALLINT NOT NULL,
                doc_no VARCHAR(30) NOT NULL,
                item_code VARCHAR(50) NOT NULL,
                item_name VARCHAR(255) NOT NULL,
                unit_code VARCHAR(50) NOT NULL,
                qty NUMERIC(18,6) NOT NULL,
                wh_code VARCHAR(50) NOT NULL,
                shelf_code VARCHAR(50) NOT NULL,
                stand_value NUMERIC(18,6) NOT NULL,
                divide_value NUMERIC(18,6) NOT NULL,
                tax_type SMALLINT NOT NULL DEFAULT 0,
                line_number INT NOT NULL
            )
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_inventory (code, name_1, unit_cost, tax_type)
            VALUES ('MAT-A', 'ERP Material A', '', 7),
                   ('MAT-B', 'ERP Material B', '', 8)
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_unit_use (code, ic_code, name_1, stand_value, divide_value, ratio, line_number)
            VALUES ('KG', 'MAT-A', 'Kilogram', 1000, 1, 1, 1),
                   ('PCS', 'MAT-B', 'Piece', 1, 12, 1, 2)
            """);
    }

    [Fact]
    public async Task SaveProductionDocumentAsync_WritesIcTransAndIcTransDetail()
    {
        var repo = new ErpProductionRepository(DbContext);
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2024, 1, 15),
            DocNo: "BP-20240115-00001",
            DocTime: new TimeOnly(9, 30, 0),
            Orders: [],
            Details:
            [
                new BomProductionDetailDto(Guid.NewGuid(), "BP-20240115-00001", "MAT-A", "Material A", 12.5m, "KG", "WH-A", "SH-01"),
                new BomProductionDetailDto(Guid.NewGuid(), "BP-20240115-00001", "MAT-B", "Material B", 3m, "PCS", "WH-B", "SH-02")
            ]);

        await repo.SaveProductionDocumentAsync(document);

        var headerCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'BP-20240115-00001'")
            .SingleAsync();
        var detailCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans_detail WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'BP-20240115-00001'")
            .SingleAsync();
        var firstLineQty = await DbContext.Database
            .SqlQueryRaw<decimal>("SELECT qty AS \"Value\" FROM ic_trans_detail WHERE doc_no = 'BP-20240115-00001' AND line_number = 1")
            .SingleAsync();
        var firstLineCalcFlag = await DbContext.Database
            .SqlQueryRaw<short>("SELECT calc_flag AS \"Value\" FROM ic_trans_detail WHERE doc_no = 'BP-20240115-00001' AND line_number = 1")
            .SingleAsync();
        var firstLineLocation = await DbContext.Database
            .SqlQueryRaw<string>("SELECT wh_code || '/' || shelf_code AS \"Value\" FROM ic_trans_detail WHERE doc_no = 'BP-20240115-00001' AND line_number = 1")
            .SingleAsync();
        var headerDocTime = await DbContext.Database
            .SqlQueryRaw<string>("SELECT doc_time AS \"Value\" FROM ic_trans WHERE doc_no = 'BP-20240115-00001'")
            .SingleAsync();
        var detailDocTime = await DbContext.Database
            .SqlQueryRaw<string>("SELECT doc_time || '/' || doc_time_calc AS \"Value\" FROM ic_trans_detail WHERE doc_no = 'BP-20240115-00001' AND line_number = 1")
            .SingleAsync();
        var firstLineMasterData = await DbContext.Database
            .SqlQueryRaw<string>("SELECT item_name || '/' || stand_value::text || '/' || divide_value::text || '/' || tax_type::text AS \"Value\" FROM ic_trans_detail WHERE doc_no = 'BP-20240115-00001' AND line_number = 1")
            .SingleAsync();

        headerCount.Should().Be(1);
        detailCount.Should().Be(2);
        firstLineQty.Should().Be(12.5m);
        firstLineCalcFlag.Should().Be(-1);
        firstLineLocation.Should().Be("WH-A/SH-01");
        headerDocTime.Should().Be("09:30");
        headerDocTime.Should().HaveLength(5);
        detailDocTime.Should().Be("09:30/09:30");
        firstLineMasterData.Should().Be("ERP Material A/1000.000000/1.000000/7");
    }

    [Fact]
    public async Task DeleteProductionDocumentAsync_RemovesIcTransAndIcTransDetailForProductionDocument()
    {
        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_trans (trans_type, trans_flag, doc_date, doc_time, doc_no)
            VALUES (3, 56, DATE '2024-01-15', '09:30', 'BP-20240115-00001'),
                   (3, 56, DATE '2024-01-15', '09:30', 'BP-20240115-00002'),
                   (3, 44, DATE '2024-01-15', '09:30', 'BP-20240115-00001')
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_trans_detail (
                trans_type,
                trans_flag,
                doc_date,
                doc_time,
                doc_date_calc,
                doc_time_calc,
                calc_flag,
                doc_no,
                item_code,
                item_name,
                unit_code,
                qty,
                wh_code,
                shelf_code,
                stand_value,
                divide_value,
                tax_type,
                line_number
            )
            VALUES (3, 56, DATE '2024-01-15', '09:30', DATE '2024-01-15', '09:30', 1, 'BP-20240115-00001', 'MAT-A', 'Material A', 'KG', 12.5, 'WH-A', 'SH-01', 1, 1, 0, 1),
                   (3, 56, DATE '2024-01-15', '09:30', DATE '2024-01-15', '09:30', 1, 'BP-20240115-00002', 'MAT-B', 'Material B', 'PCS', 3, 'WH-B', 'SH-02', 1, 1, 0, 1),
                   (3, 44, DATE '2024-01-15', '09:30', DATE '2024-01-15', '09:30', 1, 'BP-20240115-00001', 'MAT-C', 'Material C', 'PCS', 1, 'WH-C', 'SH-03', 1, 1, 0, 1)
            """);

        var repo = new ErpProductionRepository(DbContext);

        await repo.DeleteProductionDocumentAsync("BP-20240115-00001");

        var targetHeaderCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'BP-20240115-00001'")
            .SingleAsync();
        var targetDetailCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans_detail WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'BP-20240115-00001'")
            .SingleAsync();
        var untouchedCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE doc_no = 'BP-20240115-00002' OR trans_flag = 44")
            .SingleAsync();

        targetHeaderCount.Should().Be(0);
        targetDetailCount.Should().Be(0);
        untouchedCount.Should().Be(2);
    }
}
