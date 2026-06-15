using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Erp;

/// <summary>
/// EF Core implementation ของ IErpSalesOrderRepository
/// อ่านรายการขายจาก erp-database (ic_trans_detail) — read-only
/// Filter บังคับ: trans_flag = 44 AND last_status = 0
/// </summary>
public class ErpSalesOrderRepository(ErpDbContext context) : IErpSalesOrderRepository
{
    /// <summary>
    /// ดึงรายการขายจาก ic_trans_detail
    /// WHERE trans_flag = 44 AND last_status = 0
    ///   AND doc_date BETWEEN @dateFrom AND @dateTo
    /// ORDER BY doc_date, doc_no
    /// </summary>
    public async Task<IReadOnlyList<ErpSalesTransactionDto>> GetSalesTransactionsByDateRangeAsync(
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken ct = default)
    {
        var transactions = await context.IcTransDetails
            .AsNoTracking()
            .Where(t =>
                t.TransFlag == 44 &&
                t.LastStatus == 0 &&
                t.DocDate >= dateFrom &&
                t.DocDate <= dateTo)
            .OrderBy(t => t.DocDate)
            .ThenBy(t => t.DocNo)
            .ToListAsync(ct);

        return transactions.Select(t => new ErpSalesTransactionDto(
            DocDate: t.DocDate,
            DocNo: t.DocNo,
            ItemCode: t.ItemCode,
            ItemName: string.IsNullOrWhiteSpace(t.ItemName) ? t.ItemCode : t.ItemName,
            Qty: t.Qty,
            UnitCode: t.UnitCode,
            StandValue: t.StandValue,
            DivideValue: t.DivideValue,
            WhCode: t.WhCode,
            ShelfCode: t.ShelfCode)).ToList();
    }
}
