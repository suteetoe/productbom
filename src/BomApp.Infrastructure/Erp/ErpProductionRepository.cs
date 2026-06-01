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
    private const short CalculatedFlag = -1;

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

        await UpdateProductionDetailMasterDataAsync(document.DocNo, ct);

        await transaction.CommitAsync(ct);
    }

    private async Task UpdateProductionDetailMasterDataAsync(
        string docNo,
        CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ic_trans_detail
            SET
                item_name = (
                    SELECT name_1
                    FROM ic_inventory
                    WHERE ic_inventory.code = ic_trans_detail.item_code
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
              AND trans_flag = {ProductionTransFlag}
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
}
