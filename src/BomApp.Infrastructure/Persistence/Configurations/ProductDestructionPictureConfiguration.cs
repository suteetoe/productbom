using BomApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomApp.Infrastructure.Persistence.Configurations;

public class ProductDestructionPictureConfiguration : IEntityTypeConfiguration<ProductDestructionPicture>
{
    public void Configure(EntityTypeBuilder<ProductDestructionPicture> builder)
    {
        builder.ToTable("product_destruction_pictures");

        builder.HasKey(p => new { p.DocNo, p.LineNumber });

        builder.Property(p => p.DocNo)
            .HasColumnName("doc_no")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.LineNumber)
            .HasColumnName("line_number")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(p => p.ImageGuid)
            .HasColumnName("image_guid")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.ImageFile)
            .HasColumnName("image_file")
            .HasColumnType("bytea")
            .IsRequired();

        builder.HasIndex(p => p.DocNo)
            .HasDatabaseName("idx_product_destruction_pictures_doc_no");
    }
}
