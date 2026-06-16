using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

public class ProductDestructionConfiguration : IEntityTypeConfiguration<ProductDestruction>
{
    public void Configure(EntityTypeBuilder<ProductDestruction> builder)
    {
        builder.ToTable("bom_product_destruction");

        builder.HasKey(d => d.DocNo);

        builder.Property(d => d.DocNo)
            .HasColumnName("doc_no")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.DocDate)
            .HasColumnName("doc_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(d => d.WhCode)
            .HasColumnName("wh_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.ShelfCode)
            .HasColumnName("shelf_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Remark)
            .HasColumnName("remark")
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(d => d.DocDate)
            .HasDatabaseName("idx_bom_product_destruction_doc_date");

        builder.HasMany(d => d.Pictures)
            .WithOne(p => p.ProductDestruction)
            .HasForeignKey(p => p.DocNo)
            .HasPrincipalKey(d => d.DocNo)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Details)
            .WithOne(l => l.ProductDestruction)
            .HasForeignKey(l => l.DocNo)
            .HasPrincipalKey(d => d.DocNo)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
