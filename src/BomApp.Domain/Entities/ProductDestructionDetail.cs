namespace BomApp.Domain.Entities;

/// <summary>
/// Item line for a product destruction document.
/// Maps to public.product_destruction_detail.
/// </summary>
public class ProductDestructionDetail
{
    public string DocNo { get; set; } = string.Empty;

    public string ItemCode { get; set; } = string.Empty;

    public decimal Qty { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    public string WhCode { get; set; } = string.Empty;

    public string ShelfCode { get; set; } = string.Empty;

    public int LineNumber { get; set; }

    public ProductDestruction? ProductDestruction { get; set; }
}
