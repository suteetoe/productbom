namespace BomApp.Domain.Entities;

/// <summary>
/// Product manufacturing document header.
/// Maps to public.bom_material_process.
/// </summary>
public class ProductManufacturing
{
    public string DocNo { get; set; } = string.Empty;

    public DateOnly DocDate { get; set; }

    public string WhCode { get; set; } = string.Empty;

    public string ShelfCode { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public decimal TotalCost { get; set; }

    public ICollection<ProductManufacturingMaterial> Materials { get; set; } = new List<ProductManufacturingMaterial>();

    public ICollection<ProductManufacturingFinishGood> FinishGoods { get; set; } = new List<ProductManufacturingFinishGood>();
}
