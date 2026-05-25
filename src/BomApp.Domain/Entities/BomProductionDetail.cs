namespace BomApp.Domain.Entities;

/// <summary>
/// Material/item requirement calculated from BOM expansion.
/// Maps to public.bom_production_details table.
/// </summary>
public class BomProductionDetail
{
    public Guid Id { get; set; }

    public string DocNo { get; set; } = string.Empty;

    public string ItemCode { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public decimal Qty { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    public BomProduction? Production { get; set; }
}
