using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration สำหรับ BomAssignment entity → bom.bom_assignments table
/// </summary>
public class BomAssignmentConfiguration : IEntityTypeConfiguration<BomAssignment>
{
    public void Configure(EntityTypeBuilder<BomAssignment> builder)
    {
        builder.ToTable("bom_assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.ItemCode)
            .HasColumnName("item_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.BomId)
            .HasColumnName("bom_id")
            .IsRequired();

        builder.Property(a => a.AssignedAt)
            .HasColumnName("assigned_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(a => a.AssignedBy)
            .HasColumnName("assigned_by")
            .HasMaxLength(100)
            .IsRequired();

        // UNIQUE: 1 item → 1 BOM เท่านั้น
        builder.HasIndex(a => a.ItemCode)
            .IsUnique()
            .HasDatabaseName("idx_bom_assignments_item_code");

        builder.HasIndex(a => a.BomId)
            .HasDatabaseName("idx_bom_assignments_bom_id");

        // FK → boms
        builder.HasOne(a => a.Bom)
            .WithMany()
            .HasForeignKey(a => a.BomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
