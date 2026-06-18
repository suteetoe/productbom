using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

public class ProductManufacturingConfiguration : IEntityTypeConfiguration<ProductManufacturing>
{
    public void Configure(EntityTypeBuilder<ProductManufacturing> builder)
    {
        builder.ToTable("bom_material_process");

        builder.HasKey(d => d.DocNo);

        builder.Property(d => d.DocNo)
            .HasColumnName("doc_no")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.DocDate)
            .HasColumnName("doc_date")
            .HasMaxLength(10)
            .HasConversion(
                d => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                s => DateOnly.ParseExact(s, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
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

        builder.Property(d => d.TotalCost)
            .HasColumnName("total_cost")
            .HasColumnType("numeric")
            .IsRequired();

        builder.HasIndex(d => d.DocDate)
            .HasDatabaseName("idx_bom_material_process_doc_date");

        builder.HasMany(d => d.Materials)
            .WithOne(l => l.ProductManufacturing)
            .HasForeignKey(l => l.DocNo)
            .HasPrincipalKey(d => d.DocNo)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.FinishGoods)
            .WithOne(l => l.ProductManufacturing)
            .HasForeignKey(l => l.DocNo)
            .HasPrincipalKey(d => d.DocNo)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
