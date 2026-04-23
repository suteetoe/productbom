using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface สำหรับ ERP Sales Orders — อ่านจาก erp-database (ic_trans_detail)
/// อยู่ใน Application layer ตาม Clean Architecture
/// Implementation อยู่ใน Infrastructure/Erp/ErpSalesOrderRepository.cs
/// </summary>
public interface IErpSalesOrderRepository
{
    /// <summary>
    /// ดึงรายการขายจาก ic_trans_detail
    /// WHERE trans_flag = 44 AND last_status = 0
    ///   AND doc_date BETWEEN @dateFrom AND @dateTo
    /// </summary>
    Task<IReadOnlyList<ErpSalesTransactionDto>> GetSalesTransactionsByDateRangeAsync(
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken ct = default);
}
