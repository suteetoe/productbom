using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces;

/// <summary>
/// Service สำหรับจัดการ BOM Assignment — ผูก/ถอด BOM กับ Product Item จาก ERP
/// ใช้ใน BomAssignmentViewModel (UI) เพื่อ load Active BOMs, assign, remove
/// </summary>
public interface IBomAssignmentService
{
    /// <summary>Returns all Active BOMs — used for assignment dropdown selector</summary>
    Task<Result<IReadOnlyList<BomDto>>> GetActiveBomsAsync(CancellationToken ct = default);

    /// <summary>Returns the BOM currently assigned to itemCode, or null if none assigned</summary>
    Task<Result<BomDto?>> GetAssignedBomAsync(string itemCode, CancellationToken ct = default);

    /// <summary>Returns assigned BOM ids for the requested item codes.</summary>
    Task<Result<IReadOnlyDictionary<string, Guid>>> GetAssignedItemCodesAsync(
        IReadOnlyList<string> itemCodes,
        CancellationToken ct = default);

    /// <summary>
    /// Assigns bomId to itemCode — validates BOM status = Active before assigning.
    /// Overrides any existing assignment (UPSERT via IBomAssignmentRepository.AssignAsync).
    /// </summary>
    Task<Result> AssignAsync(
        string itemCode,
        string itemName,
        Guid bomId,
        string assignedBy,
        CancellationToken ct = default);

    /// <summary>Removes the BOM assignment for itemCode — no-op if not assigned</summary>
    Task<Result> RemoveAsync(string itemCode, CancellationToken ct = default);
}
