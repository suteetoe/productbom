using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for material requirements → public.bom_production_details table.
/// </summary>
public class BomProductionDetailConfiguration : IEntityTypeConfiguration<BomProductionDetail>
{
    public void Configure(EntityTypeBuilder<BomProductionDetail> builder)
    {
        builder.ToTable("bom_production_details");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.DocNo)
            .HasColumnName("doc_no")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(d => d.ItemCode)
            .HasColumnName("item_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.Qty)
            .HasColumnName("qty")
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(d => d.UnitCode)
            .HasColumnName("unit_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.WhCode)
            .HasColumnName("wh_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.ShelfCode)
            .HasColumnName("shelf_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(d => d.DocNo)
            .HasDatabaseName("idx_bom_production_details_doc_no");

        builder.HasIndex(d => d.ItemCode)
            .HasDatabaseName("idx_bom_production_details_item_code");
    }
}
