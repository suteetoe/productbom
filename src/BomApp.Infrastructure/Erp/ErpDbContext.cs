using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Erp;

/// <summary>
/// EF Core DbContext สำหรับ ERP database — read-only
/// ใช้ keyless entities สำหรับ ic_inventory, ic_unit_use, ic_trans_detail
/// Repositories ใช้ raw SQL queries ผ่าน context.Database.SqlQueryRaw
/// </summary>
public class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    /// <summary>Keyless entity สำหรับ ic_inventory (สินค้า/วัตถุดิบ)</summary>
    public DbSet<IcInventory> IcInventories { get; set; }

    /// <summary>Keyless entity สำหรับ ic_unit_use (อัตราส่วนหน่วยนับ)</summary>
    public DbSet<IcUnitUse> IcUnitUses { get; set; }

    /// <summary>Keyless entity สำหรับ ic_trans_detail (รายการขาย)</summary>
    public DbSet<IcTransDetail> IcTransDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IcInventory>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("ic_inventory");

            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(25);
            entity.Property(e => e.Name1).HasColumnName("name_1").HasMaxLength(255);
            entity.Property(e => e.UnitCost).HasColumnName("unit_cost");
        });

        modelBuilder.Entity<IcUnitUse>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("ic_unit_use");

            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(25);
            entity.Property(e => e.IcCode).HasColumnName("ic_code").HasMaxLength(25);
            entity.Property(e => e.UnitName).HasColumnName("name_1").HasMaxLength(255);
            entity.Property(e => e.StandValue).HasColumnName("stand_value");
            entity.Property(e => e.DivideValue).HasColumnName("divide_value");
            entity.Property(e => e.Ratio).HasColumnName("ratio");
            entity.Property(e => e.LineNumber).HasColumnName("line_number");
        });

        modelBuilder.Entity<IcTransDetail>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("ic_trans_detail");

            entity.Property(e => e.DocDate).HasColumnName("doc_date");
            entity.Property(e => e.DocNo).HasColumnName("doc_no").HasMaxLength(50);
            entity.Property(e => e.TransFlag).HasColumnName("trans_flag");
            entity.Property(e => e.LastStatus).HasColumnName("last_status");
            entity.Property(e => e.ItemCode).HasColumnName("item_code").HasMaxLength(50);
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.UnitCode).HasColumnName("unit_code").HasMaxLength(50);
            entity.Property(e => e.StandValue).HasColumnName("stand_value");
            entity.Property(e => e.DivideValue).HasColumnName("divide_value");
            entity.Property(e => e.WhCode).HasColumnName("wh_code").HasMaxLength(50);
            entity.Property(e => e.ShelfCode).HasColumnName("shelf_code").HasMaxLength(50);
        });
    }
}

/// <summary>Keyless entity — ic_inventory</summary>
public class IcInventory
{
    public string Code { get; set; } = string.Empty;
    public string Name1 { get; set; } = string.Empty;
    public string UnitCost { get; set; } = string.Empty;
}

/// <summary>Keyless entity — ic_unit_use (JOIN ic_unit ใน repository)</summary>
public class IcUnitUse
{
    public string Code { get; set; } = string.Empty;
    public string IcCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal StandValue { get; set; }
    public decimal DivideValue { get; set; }
    public int Ratio { get; set; }
    public int LineNumber { get; set; }
}

/// <summary>Keyless entity — ic_trans_detail</summary>
public class IcTransDetail
{
    public DateOnly DocDate { get; set; }
    public string DocNo { get; set; } = string.Empty;
    public short TransFlag { get; set; }
    public short LastStatus { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal StandValue { get; set; }
    public decimal DivideValue { get; set; }
    public string WhCode { get; set; } = string.Empty;
    public string ShelfCode { get; set; } = string.Empty;
}
