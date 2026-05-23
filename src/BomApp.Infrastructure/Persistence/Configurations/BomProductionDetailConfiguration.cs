using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration สำหรับ BomProductionDetail entity → public.bom_production_detail table
/// </summary>
public class BomProductionDetailConfiguration : IEntityTypeConfiguration<BomProductionDetail>
{
    public void Configure(EntityTypeBuilder<BomProductionDetail> builder)
    {
        builder.ToTable("bom_production_detail");

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

        builder.Property(d => d.Qty)
            .HasColumnName("qty")
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(d => d.UnitCode)
            .HasColumnName("unit_code")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(d => d.DocNo)
            .HasDatabaseName("idx_bom_production_detail_doc_no");
    }
}
