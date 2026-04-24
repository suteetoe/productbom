using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration สำหรับ Bom entity → public.bom_boms table
/// </summary>
public class BomConfiguration : IEntityTypeConfiguration<Bom>
{
    public void Configure(EntityTypeBuilder<Bom> builder)
    {
        builder.ToTable("bom_boms");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(b => b.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(b => b.ItemCode)
            .HasColumnName("item_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(b => b.YieldQuantity)
            .HasColumnName("yield_quantity")
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(b => b.YieldUnit)
            .HasColumnName("yield_unit")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Version)
            .HasColumnName("version")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("Draft")
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(b => b.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsRequired();

        // Unique constraint on code
        builder.HasIndex(b => b.Code)
            .IsUnique()
            .HasDatabaseName("idx_boms_code");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("idx_boms_status");

        // Relationships
        builder.HasMany(b => b.Lines)
            .WithOne(l => l.Bom)
            .HasForeignKey(l => l.BomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
