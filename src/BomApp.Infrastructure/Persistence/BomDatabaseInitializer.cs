using BomApp.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Persistence;

public sealed class BomDatabaseInitializer(BomDbContext context) : IBomDatabaseInitializer
{
    public Task EnsureReadyAsync(CancellationToken ct = default) =>
        context.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE IF EXISTS public.bom_production_orders
            ADD COLUMN IF NOT EXISTS item_name character varying(255) NOT NULL DEFAULT '';
            """,
            ct);
}
