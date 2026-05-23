using BomApp.Application.Interfaces.Repositories;
using BomApp.Domain.Entities;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BomApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation สำหรับเอกสาร bom_production
/// </summary>
public class BomProductionRepository(BomDbContext context) : IBomProductionRepository
{
    public async Task<BomProductionDto> CreateAsync(
        CreateBomProductionInternalCommand cmd,
        CancellationToken ct = default)
    {
        var docNo = await GenerateDocNoAsync(cmd.DocDate, ct);

        var production = new BomProduction
        {
            Id = Guid.NewGuid(),
            DocDate = cmd.DocDate,
            DocNo = docNo,
            DocTime = cmd.DocTime,
            Details = cmd.Details.Select(d => new BomProductionDetail
            {
                Id = Guid.NewGuid(),
                DocNo = docNo,
                ItemCode = d.ItemCode,
                Qty = d.Qty,
                UnitCode = d.UnitCode
            }).ToList()
        };

        context.BomProductions.Add(production);
        await context.SaveChangesAsync(ct);

        return MapToDto(production);
    }

    private async Task<string> GenerateDocNoAsync(DateOnly docDate, CancellationToken ct)
    {
        var prefix = $"BP-{docDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-";

        var lastDocNo = await context.BomProductions
            .AsNoTracking()
            .Where(p => p.DocNo.StartsWith(prefix))
            .OrderByDescending(p => p.DocNo)
            .Select(p => p.DocNo)
            .FirstOrDefaultAsync(ct);

        var nextSeq = 1;
        if (lastDocNo is not null)
        {
            var lastSeqText = lastDocNo[prefix.Length..];
            if (int.TryParse(lastSeqText, out var lastSeq))
                nextSeq = lastSeq + 1;
        }

        return $"{prefix}{nextSeq:D5}";
    }

    private static BomProductionDto MapToDto(BomProduction production) => new(
        Id: production.Id,
        DocDate: production.DocDate,
        DocNo: production.DocNo,
        DocTime: production.DocTime,
        Details: production.Details.Select(d => new BomProductionDetailDto(
            Id: d.Id,
            DocNo: d.DocNo,
            ItemCode: d.ItemCode,
            Qty: d.Qty,
            UnitCode: d.UnitCode)).ToList());
}
