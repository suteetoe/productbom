using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BomApp.Infrastructure.Persistence;

public class BomDbContextFactory : IDesignTimeDbContextFactory<BomDbContext>
{
    public BomDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseNpgsql("Host=localhost;Database=bom_dev;Username=postgres;Password=postgres;SearchPath=bom")
            .Options;
        return new BomDbContext(options);
    }
}
