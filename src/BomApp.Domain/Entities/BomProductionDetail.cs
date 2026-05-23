namespace BomApp.Domain.Entities;

/// <summary>
/// Detail เอกสารเบิกรายการสินค้าที่ผลิต
/// Maps to public.bom_production_detail table
/// </summary>
public class BomProductionDetail
{
    public Guid Id { get; set; }

    public string DocNo { get; set; } = string.Empty;

    public string ItemCode { get; set; } = string.Empty;

    public decimal Qty { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    public BomProduction? Production { get; set; }
}
