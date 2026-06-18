namespace BomApp.Domain.Entities;

/// <summary>
/// Material usage line for a product manufacturing document.
/// Maps to public.bom_material_process_use.
/// </summary>
public class ProductManufacturingMaterial
{
    public string DocNo { get; set; } = string.Empty;

    public string ItemCode { get; set; } = string.Empty;

    public decimal Qty { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    public string WhCode { get; set; } = string.Empty;

    public string ShelfCode { get; set; } = string.Empty;

    public decimal CostPerUnit { get; set; }

    public decimal TotalCost { get; set; }

    public int LineNumber { get; set; }

    public ProductManufacturing? ProductManufacturing { get; set; }
}
