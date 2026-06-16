namespace BomApp.Domain.Entities;

/// <summary>
/// Image attached to a product destruction document.
/// Maps to public.bom_product_destruction_pictures.
/// </summary>
public class ProductDestructionPicture
{
    public string DocNo { get; set; } = string.Empty;

    public short LineNumber { get; set; }

    public string ImageGuid { get; set; } = string.Empty;

    public byte[] ImageFile { get; set; } = [];

    public ProductDestruction? ProductDestruction { get; set; }
}
