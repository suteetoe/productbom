using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext สำหรับ BOM domain — ใช้ schema "bom" บน bom-database connection
/// </summary>
public class BomDbContext(DbContextOptions<BomDbContext> options) : DbContext(options)
{
    /// <summary>BOM headers</summary>
    public DbSet<Bom> Boms { get; set; }

    /// <summary>BOM lines (วัตถุดิบในสูตร)</summary>
    public DbSet<BomLine> BomLines { get; set; }

    /// <summary>BOM assignments (product item → BOM)</summary>
    public DbSet<BomAssignment> BomAssignments { get; set; }

    /// <summary>Production orders</summary>
    public DbSet<ProductionOrder> ProductionOrders { get; set; }

    /// <summary>Production order lines (วัตถุดิบจริงที่ต้องใช้)</summary>
    public DbSet<ProductionOrderLine> ProductionOrderLines { get; set; }

    /// <summary>Audit logs</summary>
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bom");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BomDbContext).Assembly);
    }
}
