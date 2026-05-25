namespace BomApp.Domain.Entities;

/// <summary>
/// Sales document item captured during production calculation.
/// Maps to public.bom_production_orders table.
/// </summary>
public class BomProductionOrder
{
    public Guid Id { get; set; }

    public string DocNo { get; set; } = string.Empty;

    public DateOnly DocDate { get; set; }

    public string RefDocNo { get; set; } = string.Empty;

    public DateOnly RefDocDate { get; set; }

    public string ItemCode { get; set; } = string.Empty;

    public decimal Qty { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    public BomProduction? Production { get; set; }
}
