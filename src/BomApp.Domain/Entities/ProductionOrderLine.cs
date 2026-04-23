namespace BomApp.Domain.Entities;

/// <summary>
/// Production Order Line entity — วัตถุดิบที่ต้องใช้จริงในคำสั่งผลิต
/// Maps to bom.production_order_lines table
/// </summary>
public class ProductionOrderLine
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>FK → production_orders.id (CASCADE DELETE)</summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>รหัสวัตถุดิบ — ref ic_inventory.code</summary>
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>ชื่อวัตถุดิบ (denormalized)</summary>
    public string MaterialName { get; set; } = string.Empty;

    /// <summary>ปริมาณที่ต้องใช้ (คำนวณแล้ว)</summary>
    public decimal RequiredQuantity { get; set; }

    /// <summary>หน่วยนับ</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Navigation property → Production Order</summary>
    public ProductionOrder? ProductionOrder { get; set; }
}
