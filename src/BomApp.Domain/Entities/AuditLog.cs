namespace BomApp.Domain.Entities;

/// <summary>
/// Audit Log entity — บันทึกการเปลี่ยนแปลงทุก operation สำคัญ
/// Maps to bom.audit_logs table
/// </summary>
public class AuditLog
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>ประเภท entity: Bom | BomLine | BomAssignment | ProductionOrder | etc.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>ID ของ entity ที่เปลี่ยนแปลง</summary>
    public Guid EntityId { get; set; }

    /// <summary>Action ที่เกิดขึ้น: Create | Update | Delete | Activate | Deactivate | Cancel</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>ผู้ทำการเปลี่ยนแปลง</summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>วันที่เวลาที่เปลี่ยนแปลง</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>ค่าก่อนเปลี่ยนแปลง (JSONB)</summary>
    public string? OldValues { get; set; }

    /// <summary>ค่าหลังเปลี่ยนแปลง (JSONB)</summary>
    public string? NewValues { get; set; }
}
