namespace BomApp.Domain.Entities;

/// <summary>
/// BOM Line entity — รายการวัตถุดิบในสูตรการผลิต
/// Maps to bom.bom_lines table
/// </summary>
public class BomLine
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>FK → boms.id</summary>
    public Guid BomId { get; set; }

    /// <summary>รหัสวัตถุดิบจาก ERP Items — ref ic_inventory.code</summary>
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>ชื่อวัตถุดิบ (denormalized จาก ERP)</summary>
    public string MaterialName { get; set; } = string.Empty;

    /// <summary>ปริมาณที่ใช้ — ต้อง > 0</summary>
    public decimal Quantity { get; set; }

    /// <summary>หน่วยนับ — ref ic_unit.code</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>FK → boms.id (ถ้าเป็น sub-assembly) — nullable</summary>
    public Guid? SubBomId { get; set; }

    /// <summary>ลำดับแสดงผล</summary>
    public int SortOrder { get; set; }

    /// <summary>หมายเหตุ</summary>
    public string? Notes { get; set; }

    /// <summary>Navigation property → parent BOM</summary>
    public Bom? Bom { get; set; }

    /// <summary>Navigation property → sub BOM (ถ้ามี)</summary>
    public Bom? SubBom { get; set; }
}
