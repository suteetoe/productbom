using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration สำหรับ BomLine entity → bom.bom_lines table
/// </summary>
public class BomLineConfiguration : IEntityTypeConfiguration<BomLine>
{
    public void Configure(EntityTypeBuilder<BomLine> builder)
    {
        builder.ToTable("bom_lines");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(l => l.BomId)
            .HasColumnName("bom_id")
            .IsRequired();

        builder.Property(l => l.MaterialCode)
            .HasColumnName("material_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.MaterialName)
            .HasColumnName("material_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(l => l.Unit)
            .HasColumnName("unit")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.SubBomId)
            .HasColumnName("sub_bom_id");

        builder.Property(l => l.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(l => l.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        // Index on bom_id for fast line lookup
        builder.HasIndex(l => l.BomId)
            .HasDatabaseName("idx_bom_lines_bom_id");

        // FK → parent BOM (configured in BomConfiguration)
        // FK → sub BOM (self-referencing, optional)
        builder.HasOne(l => l.SubBom)
            .WithMany()
            .HasForeignKey(l => l.SubBomId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
