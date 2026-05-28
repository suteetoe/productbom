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
                    {1},
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

        await transaction.CommitAsync(ct);
    }
}
