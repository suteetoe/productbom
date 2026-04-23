namespace BomApp.Domain.Entities;

/// <summary>
/// BOM header entity — สูตรการผลิต
/// Maps to bom.boms table
/// </summary>
public class Bom
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>รหัส BOM — ต้อง unique ทั้งระบบ เช่น BOM-001</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>ชื่อสูตรการผลิต</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>คำอธิบายเพิ่มเติม</summary>
    public string? Description { get; set; }

    /// <summary>รหัสสินค้าที่ใช้สูตรนี้ — ref ic_inventory.code</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>ชื่อสินค้า (denormalized จาก ERP)</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>จำนวนที่ผลิตได้ต่อ 1 รอบ — ต้อง > 0</summary>
    public decimal YieldQuantity { get; set; }

    /// <summary>หน่วยนับที่ผลิตได้ — ref ic_unit.code</summary>
    public string YieldUnit { get; set; } = string.Empty;

    /// <summary>เวอร์ชัน — เพิ่มทุกครั้งที่แก้ไข</summary>
    public int Version { get; set; } = 1;

    /// <summary>สถานะ: Draft | Active | Inactive</summary>
    public string Status { get; set; } = "Draft";

    /// <summary>วันที่สร้าง</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>วันที่แก้ไขล่าสุด</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>ผู้สร้าง</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>รายการวัตถุดิบในสูตร</summary>
    public ICollection<BomLine> Lines { get; set; } = new List<BomLine>();
}
