using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BomApp.Infrastructure.Erp;

/// <summary>
/// Writes production issue documents to ERP ic_trans and ic_trans_detail.
/// </summary>
public class ErpProductionRepository(ErpDbContext context) : IErpProductionRepository
{
    private const short ProductionTransType = 3;
    private const short ProductionTransFlag = 56;
    private const short ProductManufacturingReceiveTransFlag = 60;
    private const short CalculatedFlag = -1;
    private const short FinishedGoodsCalcFlag = 1;

    public async Task SaveProductionDocumentAsync(
        BomProductionDto document,
        CancellationToken ct = default)
    {
        var docTime = document.DocTime.ToString("HH:mm", CultureInfo.InvariantCulture);

        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO ic_trans (
                trans_type,
                trans_flag,
                doc_date,
                doc_time,
                doc_no
            )
            VALUES (
                {ProductionTransType},
                {ProductionTransFlag},
                {document.DocDate},
                {docTime},
                {document.DocNo}
            )
            """, ct);

        for (var index = 0; index < document.Details.Count; index++)
        {
            var detail = document.Details[index];
            var lineNumber = index + 1;

            await context.Database.ExecuteSqlInterpolatedAsync($"""
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
                    line_number
                )
                VALUES (
                    {ProductionTransType},
                    {ProductionTransFlag},
                    {document.DocDate},
                    {docTime},
                    {document.DocDate},
                    {docTime},
                    {CalculatedFlag},
                    {document.DocNo},
                    {detail.ItemCode},
                    {detail.ItemName},
                    {detail.UnitCode},
                    {detail.Qty},
                    {detail.WhCode},
                    {detail.ShelfCode},
                    {1m},
                    {1m},
                    {lineNumber}
                )
                """, ct);
        }

        await UpdateProductionDetailMasterDataAsync(document.DocNo, ProductionTransFlag, ct);

        await transaction.CommitAsync(ct);
    }

    public async Task SaveProductDestructionDocumentAsync(
        ProductDestructionDto document,
        CancellationToken ct = default)
    {
        var docTime = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);

        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM sml_doc_images
            WHERE image_id = {document.DocNo}
            """, ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ic_trans_detail
            WHERE trans_type = {ProductionTransType}
              AND trans_flag = {ProductionTransFlag}
              AND doc_no = {document.DocNo}
            """, ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ic_trans
            WHERE trans_type = {ProductionTransType}
              AND trans_flag = {ProductionTransFlag}
              AND doc_no = {document.DocNo}
            """, ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO ic_trans (
                trans_type,
                trans_flag,
                doc_date,
                doc_time,
                doc_no
            )
            VALUES (
                {ProductionTransType},
                {ProductionTransFlag},
                {document.DocDate},
                {docTime},
                {document.DocNo}
            )
            """, ct);

        foreach (var detail in document.Details.OrderBy(d => d.LineNumber))
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
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
                    line_number
                )
                VALUES (
                    {ProductionTransType},
                    {ProductionTransFlag},
                    {document.DocDate},
                    {docTime},
                    {document.DocDate},
                    {docTime},
                    {CalculatedFlag},
                    {document.DocNo},
                    {detail.ItemCode},
                    {detail.ItemName},
                    {detail.UnitCode},
                    {detail.Qty},
                    {detail.WhCode},
                    {detail.ShelfCode},
                    {1m},
                    {1m},
                    {detail.LineNumber}
                )
                """, ct);
        }

        foreach (var picture in document.Pictures.OrderBy(p => p.LineNumber))
        {
            var guidCode = Guid.Parse(picture.ImageGuid);

            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO sml_doc_images (
                    image_id,
                    image_file,
                    guid_code
                )
                VALUES (
                    {document.DocNo},
                    {picture.ImageFile},
                    {guidCode}
                )
                """, ct);
        }

        await UpdateProductionDetailMasterDataAsync(document.DocNo, ProductionTransFlag, ct);

        await transaction.CommitAsync(ct);
    }

    public async Task SaveProductManufacturingDocumentAsync(
        ProductManufacturingDto document,
        CancellationToken ct = default)
    {
        var docTime = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);

        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        await DeleteIcTransDocumentAsync(document.DocNo, ProductionTransFlag, ct);
        await DeleteIcTransDocumentAsync(document.DocNo, ProductManufacturingReceiveTransFlag, ct);

        await InsertIcTransHeaderAsync(document.DocNo, document.DocDate, docTime, ProductionTransFlag, ct);
        foreach (var material in document.Materials.OrderBy(d => d.LineNumber))
        {
            await InsertIcTransDetailAsync(
                document.DocNo,
                document.DocDate,
                docTime,
                ProductionTransFlag,
                material.ItemCode,
                material.ItemName,
                material.UnitCode,
                material.Qty,
                material.WhCode,
                material.ShelfCode,
                material.LineNumber,
                material.CostPerUnit,
                material.TotalCost,
                CalculatedFlag,
                ct);
        }

        var finishGoodTotalAmount = document.FinishGoods.Sum(d => d.TotalCost);
        await InsertIcTransHeaderAsync(
            document.DocNo,
            document.DocDate,
            docTime,
            ProductManufacturingReceiveTransFlag,
            finishGoodTotalAmount,
            ct);
        foreach (var finishGood in document.FinishGoods.OrderBy(d => d.LineNumber))
        {
            await InsertIcTransDetailAsync(
                document.DocNo,
                document.DocDate,
                docTime,
                ProductManufacturingReceiveTransFlag,
                finishGood.ItemCode,
                finishGood.ItemName,
                finishGood.UnitCode,
                finishGood.Qty,
                finishGood.WhCode,
                finishGood.ShelfCode,
                finishGood.LineNumber,
                finishGood.CostPerUnit,
                finishGood.TotalCost,
                FinishedGoodsCalcFlag,
                ct);
        }

        await UpdateProductionDetailMasterDataAsync(document.DocNo, ProductionTransFlag, ct);
        await UpdateProductionDetailMasterDataAsync(document.DocNo, ProductManufacturingReceiveTransFlag, ct);

        await transaction.CommitAsync(ct);
    }

    private async Task InsertIcTransHeaderAsync(
        string docNo,
        DateOnly docDate,
        string docTime,
        short transFlag,
        decimal totalAmount,
        CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO ic_trans (
                trans_type,
                trans_flag,
                doc_date,
                doc_time,
                doc_no,
                total_amount
            )
            VALUES (
                {ProductionTransType},
                {transFlag},
                {docDate},
                {docTime},
                {docNo},
                {totalAmount}
            )
            """, ct);
    }

    private Task InsertIcTransHeaderAsync(
        string docNo,
        DateOnly docDate,
        string docTime,
        short transFlag,
        CancellationToken ct) =>
        InsertIcTransHeaderAsync(docNo, docDate, docTime, transFlag, 0m, ct);

    private async Task InsertIcTransDetailAsync(
        string docNo,
        DateOnly docDate,
        string docTime,
        short transFlag,
        string itemCode,
        string itemName,
        string unitCode,
        decimal qty,
        string whCode,
        string shelfCode,
        int lineNumber,
        decimal costPerUnit,
        decimal totalCost,
        short calcFlag,
        CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
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
                price,
                sum_of_cost,
                sum_amount,
                sum_amount_exclude_vat,
                stand_value,
                divide_value,
                line_number
            )
            VALUES (
                {ProductionTransType},
                {transFlag},
                {docDate},
                {docTime},
                {docDate},
                {docTime},
                {calcFlag},
                {docNo},
                {itemCode},
                {itemName},
                {unitCode},
                {qty},
                {whCode},
                {shelfCode},
                {costPerUnit},
                {totalCost},
                {totalCost},
                {totalCost},
                {1m},
                {1m},
                {lineNumber}
            )
            """, ct);
    }

    private async Task UpdateProductionDetailMasterDataAsync(
        string docNo,
        short transFlag,
        CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ic_trans_detail
            SET
                item_name = COALESCE(
                    NULLIF((
                        SELECT name_1
                        FROM ic_inventory
                        WHERE ic_inventory.code = ic_trans_detail.item_code
                    ), ''),
                    NULLIF(item_name, ''),
                    item_code
                ),
                stand_value = (
                    SELECT stand_value
                    FROM ic_unit_use
                    WHERE ic_unit_use.code = ic_trans_detail.unit_code
                      AND ic_unit_use.ic_code = ic_trans_detail.item_code
                ),
                divide_value = (
                    SELECT divide_value
                    FROM ic_unit_use
                    WHERE ic_unit_use.code = ic_trans_detail.unit_code
                      AND ic_unit_use.ic_code = ic_trans_detail.item_code
                ),
                doc_date_calc = doc_date,
                doc_time_calc = doc_time,
                tax_type = (
                    SELECT tax_type
                    FROM ic_inventory
                    WHERE ic_inventory.code = ic_trans_detail.item_code
                )
            WHERE trans_type = {ProductionTransType}
              AND trans_flag = {transFlag}
              AND doc_no = {docNo}
            """, ct);
    }

    public async Task DeleteProductionDocumentAsync(
        string docNo,
        CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ic_trans_detail
            WHERE trans_type = {ProductionTransType}
              AND trans_flag = {ProductionTransFlag}
              AND doc_no = {docNo}
            """, ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ic_trans
            WHERE trans_type = {ProductionTransType}
              AND trans_flag = {ProductionTransFlag}
              AND doc_no = {docNo}
            """, ct);

        await transaction.CommitAsync(ct);
    }

    public async Task DeleteProductManufacturingDocumentAsync(
        string docNo,
        CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        await DeleteIcTransDocumentAsync(docNo, ProductionTransFlag, ct);
        await DeleteIcTransDocumentAsync(docNo, ProductManufacturingReceiveTransFlag, ct);

        await transaction.CommitAsync(ct);
    }

    public async Task DeleteProductDestructionDocumentAsync(
        string docNo,
        CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM sml_doc_images
            WHERE image_id = {docNo}
            """, ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ic_trans_detail
            WHERE trans_type = {ProductionTransType}
              AND trans_flag = {ProductionTransFlag}
              AND doc_no = {docNo}
            """, ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ic_trans
            WHERE trans_type = {ProductionTransType}
              AND trans_flag = {ProductionTransFlag}
              AND doc_no = {docNo}
            """, ct);

        await transaction.CommitAsync(ct);
    }

    private async Task DeleteIcTransDocumentAsync(
        string docNo,
        short transFlag,
        CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ic_trans_detail
            WHERE trans_type = {ProductionTransType}
              AND trans_flag = {transFlag}
              AND doc_no = {docNo}
            """, ct);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ic_trans
            WHERE trans_type = {ProductionTransType}
              AND trans_flag = {transFlag}
              AND doc_no = {docNo}
            """, ct);
    }
}
