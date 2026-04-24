using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration สำหรับ ProductionOrder entity → public.bom_production_orders table
/// สำคัญ: TEXT[] และ JSONB columns ต้อง map ถูกต้อง
/// </summary>
public class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.ToTable("bom_production_orders");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.OrderNo)
            .HasColumnName("order_no")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.BomId)
            .HasColumnName("bom_id")
            .IsRequired();

        // JSONB column — snapshot of BOM at creation time
        builder.Property(p => p.BomSnapshot)
            .HasColumnName("bom_snapshot")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(p => p.ItemCode)
            .HasColumnName("item_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("Pending")
            .IsRequired();

        // TEXT[] column — requires Npgsql array support
        builder.Property(p => p.SourceSoNumbers)
            .HasColumnName("source_so_numbers")
            .HasColumnType("text[]");

        builder.Property(p => p.SourceDocDateFrom)
            .HasColumnName("source_doc_date_from")
            .HasColumnType("date");

        builder.Property(p => p.SourceDocDateTo)
            .HasColumnName("source_doc_date_to")
            .HasColumnType("date");

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.CreatedVia)
            .HasColumnName("created_via")
            .HasMaxLength(10)
            .HasDefaultValue("UI")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(p => p.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        // Unique constraint on order_no
        builder.HasIndex(p => p.OrderNo)
            .IsUnique();

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("idx_production_orders_status");

        builder.HasIndex(p => p.CreatedAt)
            .IsDescending()
            .HasDatabaseName("idx_production_orders_created_at");

        builder.HasIndex(p => p.ItemCode)
            .HasDatabaseName("idx_production_orders_item_code");

        // GIN index สำหรับ array search บน source_so_numbers
        builder.HasIndex(p => p.SourceSoNumbers)
            .HasMethod("gin")
            .HasDatabaseName("idx_production_orders_source_so");

        // Relationships
        builder.HasOne(p => p.Bom)
            .WithMany()
            .HasForeignKey(p => p.BomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.ProductionOrder)
            .HasForeignKey(l => l.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
