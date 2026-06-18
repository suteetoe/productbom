using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

public class ProductManufacturingMaterialConfiguration : IEntityTypeConfiguration<ProductManufacturingMaterial>
{
    public void Configure(EntityTypeBuilder<ProductManufacturingMaterial> builder)
    {
        builder.ToTable("bom_material_process_use");

        builder.HasKey(d => new { d.DocNo, d.LineNumber });

        builder.Property(d => d.DocNo)
            .HasColumnName("doc_no")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.ItemCode)
            .HasColumnName("item_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Qty)
            .HasColumnName("qty")
            .HasColumnType("numeric")
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

        builder.Property(d => d.LineNumber)
            .HasColumnName("line_number")
            .IsRequired();

        builder.HasIndex(d => d.DocNo)
            .HasDatabaseName("idx_bom_material_process_use_doc_no");

        builder.HasIndex(d => d.ItemCode)
            .HasDatabaseName("idx_bom_material_process_use_item_code");
    }
}
