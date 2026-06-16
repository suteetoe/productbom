namespace BomApp.Domain.Entities;

/// <summary>
/// Product destruction document header.
/// Maps to public.bom_product_destruction.
/// </summary>
public class ProductDestruction
{
    public string DocNo { get; set; } = string.Empty;

    public DateOnly DocDate { get; set; }

    public string WhCode { get; set; } = string.Empty;

    public string ShelfCode { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public ICollection<ProductDestructionPicture> Pictures { get; set; } = new List<ProductDestructionPicture>();

    public ICollection<ProductDestructionDetail> Details { get; set; } = new List<ProductDestructionDetail>();
}
