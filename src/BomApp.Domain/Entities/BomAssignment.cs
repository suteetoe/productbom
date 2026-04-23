namespace BomApp.Domain.Entities;

/// <summary>
/// BOM Assignment entity — เชื่อม Product Item จาก ERP กับ BOM
/// Maps to bom.bom_assignments table
/// 1 item_code → 1 BOM เท่านั้น (UNIQUE constraint)
/// </summary>
public class BomAssignment
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>รหัส item จาก ERP — UNIQUE, ref ic_inventory.code</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>ชื่อสินค้า (denormalized)</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>FK → boms.id</summary>
    public Guid BomId { get; set; }

    /// <summary>วันที่ assign</summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>ผู้ assign</summary>
    public string AssignedBy { get; set; } = string.Empty;

    /// <summary>Navigation property → BOM</summary>
    public Bom? Bom { get; set; }
}
