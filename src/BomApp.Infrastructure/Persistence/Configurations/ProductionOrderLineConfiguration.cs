using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration สำหรับ ProductionOrderLine entity → public.bom_production_order_lines table
/// </summary>
public class ProductionOrderLineConfiguration : IEntityTypeConfiguration<ProductionOrderLine>
{
    public void Configure(EntityTypeBuilder<ProductionOrderLine> builder)
    {
        builder.ToTable("bom_production_order_lines");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(l => l.ProductionOrderId)
            .HasColumnName("production_order_id")
            .IsRequired();

        builder.Property(l => l.MaterialCode)
            .HasColumnName("material_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.MaterialName)
            .HasColumnName("material_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.RequiredQuantity)
            .HasColumnName("required_quantity")
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(l => l.Unit)
            .HasColumnName("unit")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(l => l.ProductionOrderId)
            .HasDatabaseName("idx_po_lines_production_order_id");

        // FK → production_orders (configured in ProductionOrderConfiguration via HasMany)
    }
}
