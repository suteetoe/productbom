using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;

namespace BomApp.Tests.Fakes;

public class FakeErpSalesOrderRepository : IErpSalesOrderRepository
{
    private readonly List<ErpSalesTransactionDto> _transactions = new()
    {
        SeedData.Sales_Day1_Doc1_PROD001,
        SeedData.Sales_Day1_Doc2_PROD001,
        SeedData.Sales_Day1_Doc1_PROD999,
        SeedData.Sales_Day2_Doc1_PROD002
    };

    public Task<IReadOnlyList<ErpSalesTransactionDto>> GetSalesTransactionsByDateRangeAsync(
        DateOnly dateFrom, DateOnly dateTo, CancellationToken ct = default)
    {
        var result = _transactions
            .Where(t => t.DocDate >= dateFrom && t.DocDate <= dateTo)
            .ToList();
        return Task.FromResult<IReadOnlyList<ErpSalesTransactionDto>>(result);
    }
}
