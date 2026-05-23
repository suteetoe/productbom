using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration สำหรับ BomProduction entity → public.bom_production table
/// </summary>
public class BomProductionConfiguration : IEntityTypeConfiguration<BomProduction>
{
    public void Configure(EntityTypeBuilder<BomProduction> builder)
    {
        builder.ToTable("bom_production");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.DocDate)
            .HasColumnName("doc_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(p => p.DocNo)
            .HasColumnName("doc_no")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.DocTime)
            .HasColumnName("doc_time")
            .HasColumnType("time")
            .IsRequired();

        builder.HasIndex(p => p.DocNo)
            .IsUnique()
            .HasDatabaseName("idx_bom_production_doc_no");

        builder.HasIndex(p => p.DocDate)
            .HasDatabaseName("idx_bom_production_doc_date");

        builder.HasMany(p => p.Details)
            .WithOne(d => d.Production)
            .HasPrincipalKey(p => p.DocNo)
            .HasForeignKey(d => d.DocNo)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
