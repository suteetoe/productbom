using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext สำหรับ BOM domain — ใช้ schema "public" บน bom-database connection (shared with ERP)
/// </summary>
public class BomDbContext(DbContextOptions<BomDbContext> options) : DbContext(options)
{
    /// <summary>BOM headers</summary>
    public DbSet<Bom> Boms { get; set; }

    /// <summary>BOM lines (วัตถุดิบในสูตร)</summary>
    public DbSet<BomLine> BomLines { get; set; }

    /// <summary>BOM assignments (product item → BOM)</summary>
    public DbSet<BomAssignment> BomAssignments { get; set; }

    /// <summary>Production calculation document headers</summary>
    public DbSet<BomProduction> BomProductions { get; set; }

    /// <summary>Sales rows captured for production calculation</summary>
    public DbSet<BomProductionOrder> BomProductionOrders { get; set; }

    /// <summary>Material requirements calculated from BOM expansion</summary>
    public DbSet<BomProductionDetail> BomProductionDetails { get; set; }

    /// <summary>Product destruction document headers</summary>
    public DbSet<ProductDestruction> ProductDestructions { get; set; }

    /// <summary>Product destruction attached pictures</summary>
    public DbSet<ProductDestructionPicture> ProductDestructionPictures { get; set; }

    /// <summary>Product destruction item lines</summary>
    public DbSet<ProductDestructionDetail> ProductDestructionDetails { get; set; }

    /// <summary>Direct product manufacturing document headers</summary>
    public DbSet<ProductManufacturing> ProductManufacturings { get; set; }

    /// <summary>Direct product manufacturing material usage lines</summary>
    public DbSet<ProductManufacturingMaterial> ProductManufacturingMaterials { get; set; }

    /// <summary>Direct product manufacturing finished good lines</summary>
    public DbSet<ProductManufacturingFinishGood> ProductManufacturingFinishGoods { get; set; }

    /// <summary>Audit logs</summary>
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BomDbContext).Assembly);
    }
}
