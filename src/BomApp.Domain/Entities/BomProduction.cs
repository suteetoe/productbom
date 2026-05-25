namespace BomApp.Domain.Entities;

/// <summary>
/// Production calculation document header.
/// Maps to public.bom_productions table.
/// </summary>
public class BomProduction
{
    public Guid Id { get; set; }

    public DateOnly DocDate { get; set; }

    public string DocNo { get; set; } = string.Empty;

    public TimeOnly DocTime { get; set; }

    public ICollection<BomProductionOrder> Orders { get; set; } = new List<BomProductionOrder>();

    public ICollection<BomProductionDetail> Details { get; set; } = new List<BomProductionDetail>();
}
