namespace BomApp.Domain.Entities;

/// <summary>
/// Production Order entity — คำสั่งผลิต
/// Maps to bom.production_orders table
/// </summary>
public class ProductionOrder
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>เลขที่ Production Order — format PO-YYYYMM-NNNNN — UNIQUE</summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>FK → boms.id</summary>
    public Guid BomId { get; set; }

    /// <summary>Snapshot ของ BOM ณ เวลาสร้าง (JSONB) — ป้องกันผลกระทบเมื่อ BOM เปลี่ยน</summary>
    public string BomSnapshot { get; set; } = string.Empty;

    /// <summary>รหัสสินค้าที่ผลิต</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>ชื่อสินค้าที่ผลิต</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>จำนวนที่สั่งผลิต — ต้อง > 0</summary>
    public decimal Quantity { get; set; }

    /// <summary>สถานะ: Pending | Processing | Done | Cancelled</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>doc_no จาก ic_trans_detail ที่นำมาคำนวณ (TEXT[])</summary>
    public string[] SourceSoNumbers { get; set; } = [];

    /// <summary>วันที่เริ่มต้นของ source documents</summary>
    public DateOnly? SourceDocDateFrom { get; set; }

    /// <summary>วันที่สิ้นสุดของ source documents</summary>
    public DateOnly? SourceDocDateTo { get; set; }

    /// <summary>ผู้สร้าง — username หรือ "SYSTEM" (CLI)</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>ช่องทางสร้าง: "UI" | "CLI"</summary>
    public string CreatedVia { get; set; } = "UI";

    /// <summary>วันที่สร้าง</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>หมายเหตุ</summary>
    public string? Notes { get; set; }

    /// <summary>Navigation property → BOM</summary>
    public Bom? Bom { get; set; }

    /// <summary>รายการวัตถุดิบที่ต้องใช้</summary>
    public ICollection<ProductionOrderLine> Lines { get; set; } = new List<ProductionOrderLine>();
}
