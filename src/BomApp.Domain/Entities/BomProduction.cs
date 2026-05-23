namespace BomApp.Domain.Entities;

/// <summary>
/// Header เอกสารเบิกรายการสินค้าที่ผลิต
/// Maps to public.bom_production table
/// </summary>
public class BomProduction
{
    public Guid Id { get; set; }

    public DateOnly DocDate { get; set; }

    public string DocNo { get; set; } = string.Empty;

    public TimeOnly DocTime { get; set; }

    public ICollection<BomProductionDetail> Details { get; set; } = new List<BomProductionDetail>();
}
