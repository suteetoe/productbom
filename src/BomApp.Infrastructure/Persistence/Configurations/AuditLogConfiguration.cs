using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration สำหรับ AuditLog entity → bom.audit_logs table
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.ChangedBy)
            .HasColumnName("changed_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.ChangedAt)
            .HasColumnName("changed_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(a => a.OldValues)
            .HasColumnName("old_values")
            .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
            .HasColumnName("new_values")
            .HasColumnType("jsonb");

        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("idx_audit_entity");

        builder.HasIndex(a => a.ChangedAt)
            .IsDescending()
            .HasDatabaseName("idx_audit_changed_at");
    }
}
