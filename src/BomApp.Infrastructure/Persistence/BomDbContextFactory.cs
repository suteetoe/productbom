using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BomApp.Infrastructure.Persistence;

public class BomDbContextFactory : IDesignTimeDbContextFactory<BomDbContext>
{
    public BomDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseNpgsql(
                "Host=192.168.2.212;Port=5432;Database=productbom;Username=postgres;Password=sml",
                o => o.MigrationsHistoryTable("__EFMigrationsHistory", "public"))
            .Options;
        return new BomDbContext(options);
    }
}
