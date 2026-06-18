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
            CREATE TABLE IF NOT EXISTS sml_doc_images (
                image_id VARCHAR(50) NOT NULL,
                image_file BYTEA NOT NULL,
                guid_code UUID NOT NULL
            )
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_inventory (code, name_1, unit_cost, tax_type)
            VALUES ('MAT-A', 'ERP Material A', '', 7),
                   ('MAT-B', 'ERP Material B', '', 8),
                   ('FG-001', 'ERP Finished Good', '', 9)
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_unit_use (code, ic_code, name_1, stand_value, divide_value, ratio, line_number)
            VALUES ('KG', 'MAT-A', 'Kilogram', 1000, 1, 1, 1),
                   ('PCS', 'MAT-B', 'Piece', 1, 12, 1, 2),
                   ('PCS', 'FG-001', 'Piece', 1, 1, 1, 3)
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
    public async Task SaveProductionDocumentAsync_WhenInventoryNameIsMissing_KeepsDocumentDetailName()
    {
        var repo = new ErpProductionRepository(DbContext);
        var document = new BomProductionDto(
            Id: Guid.NewGuid(),
            DocDate: new DateOnly(2024, 1, 15),
            DocNo: "BP-20240115-00003",
            DocTime: new TimeOnly(9, 30, 0),
            Orders: [],
            Details:
            [
                new BomProductionDetailDto(
                    Guid.NewGuid(),
                    "BP-20240115-00003",
                    "MAT-NO-MASTER",
                    "Document Material Name",
                    12.5m,
                    "KG",
                    "WH-A",
                    "SH-01")
            ]);

        await repo.SaveProductionDocumentAsync(document);

        var detailName = await DbContext.Database
            .SqlQueryRaw<string>("SELECT item_name AS \"Value\" FROM ic_trans_detail WHERE doc_no = 'BP-20240115-00003' AND line_number = 1")
            .SingleAsync();

        detailName.Should().Be("Document Material Name");
    }

    [Fact]
    public async Task SaveProductDestructionDocumentAsync_WritesIcTransIcTransDetailAndImages()
    {
        var repo = new ErpProductionRepository(DbContext);
        var imageGuid = Guid.NewGuid();
        var document = new ProductDestructionDto(
            DocNo: "PD-20260616-00001",
            DocDate: new DateOnly(2026, 6, 16),
            WhCode: "WH-A",
            ShelfCode: "SH-01",
            Remark: "damaged",
            Pictures:
            [
                new ProductDestructionPictureDto("PD-20260616-00001", 1, imageGuid.ToString("N"), [1, 2, 3])
            ],
            Details:
            [
                new ProductDestructionDetailDto("PD-20260616-00001", "MAT-A", "Material A", 2.5m, "KG", "WH-A", "SH-01", 1)
            ]);

        await repo.SaveProductDestructionDocumentAsync(document);

        var headerCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'PD-20260616-00001'")
            .SingleAsync();
        var detailCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans_detail WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'PD-20260616-00001'")
            .SingleAsync();
        var imageCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM sml_doc_images WHERE image_id = 'PD-20260616-00001'")
            .SingleAsync();
        var imageGuidValue = await DbContext.Database
            .SqlQueryRaw<Guid>("SELECT guid_code AS \"Value\" FROM sml_doc_images WHERE image_id = 'PD-20260616-00001'")
            .SingleAsync();
        var detailLocation = await DbContext.Database
            .SqlQueryRaw<string>("SELECT wh_code || '/' || shelf_code AS \"Value\" FROM ic_trans_detail WHERE doc_no = 'PD-20260616-00001' AND line_number = 1")
            .SingleAsync();

        headerCount.Should().Be(1);
        detailCount.Should().Be(1);
        imageCount.Should().Be(1);
        imageGuidValue.Should().Be(imageGuid);
        detailLocation.Should().Be("WH-A/SH-01");
    }

    [Fact]
    public async Task SaveProductManufacturingDocumentAsync_WritesIssueAndReceiveDocuments()
    {
        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_trans (trans_type, trans_flag, doc_date, doc_time, doc_no)
            VALUES (3, 56, DATE '2026-06-17', '08:15', 'MP-20260617-00001'),
                   (3, 60, DATE '2026-06-17', '08:15', 'MP-20260617-00001')
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
            VALUES (3, 56, DATE '2026-06-17', '08:15', DATE '2026-06-17', '08:15', -1, 'MP-20260617-00001', 'OLD-MAT', 'Old Material', 'KG', 1, 'WH-OLD', 'SH-OLD', 1, 1, 0, 1),
                   (3, 60, DATE '2026-06-17', '08:15', DATE '2026-06-17', '08:15', -1, 'MP-20260617-00001', 'OLD-FG', 'Old Finished Good', 'PCS', 1, 'WH-OLD', 'SH-OLD', 1, 1, 0, 1)
            """);

        var repo = new ErpProductionRepository(DbContext);
        var document = new ProductManufacturingDto(
            DocNo: "MP-20260617-00001",
            DocDate: new DateOnly(2026, 6, 17),
            WhCode: "WH-H",
            ShelfCode: "SH-H",
            Remark: "produce",
            FinishGoods:
            [
                new ProductManufacturingFinishGoodDto("MP-20260617-00001", "FG-001", "Finished Good", 3m, "PCS", "WH-FG", "SH-FG", 1)
            ],
            Materials:
            [
                new ProductManufacturingMaterialDto("MP-20260617-00001", "MAT-A", "Material A", 6m, "KG", "WH-RM", "SH-RM", 1)
            ]);

        await repo.SaveProductManufacturingDocumentAsync(document);

        var issueHeaderCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'MP-20260617-00001'")
            .SingleAsync();
        var receiveHeaderCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE trans_type = 3 AND trans_flag = 60 AND doc_no = 'MP-20260617-00001'")
            .SingleAsync();
        var issueDetail = await DbContext.Database
            .SqlQueryRaw<string>("SELECT item_code || '/' || qty::text || '/' || wh_code || '/' || shelf_code AS \"Value\" FROM ic_trans_detail WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'MP-20260617-00001' AND line_number = 1")
            .SingleAsync();
        var receiveDetail = await DbContext.Database
            .SqlQueryRaw<string>("SELECT item_code || '/' || qty::text || '/' || wh_code || '/' || shelf_code AS \"Value\" FROM ic_trans_detail WHERE trans_type = 3 AND trans_flag = 60 AND doc_no = 'MP-20260617-00001' AND line_number = 1")
            .SingleAsync();
        var issueCalcFlag = await DbContext.Database
            .SqlQueryRaw<short>("SELECT calc_flag AS \"Value\" FROM ic_trans_detail WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'MP-20260617-00001' AND line_number = 1")
            .SingleAsync();
        var receiveMasterData = await DbContext.Database
            .SqlQueryRaw<string>("SELECT item_name || '/' || stand_value::text || '/' || divide_value::text || '/' || tax_type::text AS \"Value\" FROM ic_trans_detail WHERE trans_type = 3 AND trans_flag = 60 AND doc_no = 'MP-20260617-00001' AND line_number = 1")
            .SingleAsync();

        issueHeaderCount.Should().Be(1);
        receiveHeaderCount.Should().Be(1);
        issueDetail.Should().Be("MAT-A/6.000000/WH-RM/SH-RM");
        receiveDetail.Should().Be("FG-001/3.000000/WH-FG/SH-FG");
        issueCalcFlag.Should().Be(-1);
        receiveMasterData.Should().Be("ERP Finished Good/1.000000/1.000000/9");
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

    [Fact]
    public async Task DeleteProductManufacturingDocumentAsync_RemovesIssueAndReceiveDocuments()
    {
        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_trans (trans_type, trans_flag, doc_date, doc_time, doc_no)
            VALUES (3, 56, DATE '2026-06-17', '09:30', 'MP-20260617-00001'),
                   (3, 60, DATE '2026-06-17', '09:30', 'MP-20260617-00001'),
                   (3, 56, DATE '2026-06-17', '09:30', 'MP-20260617-00002')
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
            VALUES (3, 56, DATE '2026-06-17', '09:30', DATE '2026-06-17', '09:30', -1, 'MP-20260617-00001', 'MAT-A', 'Material A', 'KG', 6, 'WH-RM', 'SH-RM', 1, 1, 0, 1),
                   (3, 60, DATE '2026-06-17', '09:30', DATE '2026-06-17', '09:30', -1, 'MP-20260617-00001', 'FG-001', 'Finished Good', 'PCS', 3, 'WH-FG', 'SH-FG', 1, 1, 0, 1),
                   (3, 56, DATE '2026-06-17', '09:30', DATE '2026-06-17', '09:30', -1, 'MP-20260617-00002', 'MAT-B', 'Material B', 'PCS', 1, 'WH-B', 'SH-B', 1, 1, 0, 1)
            """);

        var repo = new ErpProductionRepository(DbContext);

        await repo.DeleteProductManufacturingDocumentAsync("MP-20260617-00001");

        var targetHeaderCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE trans_type = 3 AND trans_flag IN (56, 60) AND doc_no = 'MP-20260617-00001'")
            .SingleAsync();
        var targetDetailCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans_detail WHERE trans_type = 3 AND trans_flag IN (56, 60) AND doc_no = 'MP-20260617-00001'")
            .SingleAsync();
        var untouchedCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE doc_no = 'MP-20260617-00002'")
            .SingleAsync();

        targetHeaderCount.Should().Be(0);
        targetDetailCount.Should().Be(0);
        untouchedCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteProductDestructionDocumentAsync_RemovesIcTransIcTransDetailAndImages()
    {
        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO ic_trans (trans_type, trans_flag, doc_date, doc_time, doc_no)
            VALUES (3, 56, DATE '2026-06-16', '09:30', 'PD-20260616-00001'),
                   (3, 56, DATE '2026-06-16', '09:30', 'PD-20260616-00002')
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
            VALUES (3, 56, DATE '2026-06-16', '09:30', DATE '2026-06-16', '09:30', 1, 'PD-20260616-00001', 'MAT-A', 'Material A', 'KG', 12.5, 'WH-A', 'SH-01', 1, 1, 0, 1),
                   (3, 56, DATE '2026-06-16', '09:30', DATE '2026-06-16', '09:30', 1, 'PD-20260616-00002', 'MAT-B', 'Material B', 'PCS', 3, 'WH-B', 'SH-02', 1, 1, 0, 1)
            """);

        await DbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO sml_doc_images (image_id, image_file, guid_code)
            VALUES ('PD-20260616-00001', '\x010203', '11111111-1111-1111-1111-111111111111'),
                   ('PD-20260616-00002', '\x010203', '22222222-2222-2222-2222-222222222222')
            """);

        var repo = new ErpProductionRepository(DbContext);

        await repo.DeleteProductDestructionDocumentAsync("PD-20260616-00001");

        var targetHeaderCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'PD-20260616-00001'")
            .SingleAsync();
        var targetDetailCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans_detail WHERE trans_type = 3 AND trans_flag = 56 AND doc_no = 'PD-20260616-00001'")
            .SingleAsync();
        var targetImageCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM sml_doc_images WHERE image_id = 'PD-20260616-00001'")
            .SingleAsync();
        var untouchedCount = await DbContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM ic_trans WHERE doc_no = 'PD-20260616-00002'")
            .SingleAsync();

        targetHeaderCount.Should().Be(0);
        targetDetailCount.Should().Be(0);
        targetImageCount.Should().Be(0);
        untouchedCount.Should().Be(1);
    }
}
