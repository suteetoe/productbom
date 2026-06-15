using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for captured sales rows → public.bom_production_orders table.
/// </summary>
public class BomProductionOrderConfiguration : IEntityTypeConfiguration<BomProductionOrder>
{
    public void Configure(EntityTypeBuilder<BomProductionOrder> builder)
    {
        builder.ToTable("bom_production_orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(o => o.DocNo)
            .HasColumnName("doc_no")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.DocDate)
            .HasColumnName("doc_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(o => o.RefDocNo)
            .HasColumnName("ref_doc_no")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.RefDocDate)
            .HasColumnName("ref_doc_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(o => o.ItemCode)
            .HasColumnName("item_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(o => o.Qty)
            .HasColumnName("qty")
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(o => o.UnitCode)
            .HasColumnName("unit_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(o => o.DocNo)
            .HasDatabaseName("idx_bom_production_orders_doc_no");

        builder.HasIndex(o => o.DocDate)
            .HasDatabaseName("idx_bom_production_orders_doc_date");

        builder.HasIndex(o => o.RefDocNo)
            .HasDatabaseName("idx_bom_production_orders_ref_doc_no");

        builder.HasIndex(o => o.ItemCode)
            .HasDatabaseName("idx_bom_production_orders_item_code");
    }
}
