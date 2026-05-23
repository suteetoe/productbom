using BomApp.Domain.Common;
using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces;

/// <summary>
/// Use case หลักสำหรับหน้าจอ 2.5 — คำนวณวัตถุดิบจากรายการขาย
/// เรียกได้ทั้งจาก UI (SalesCalculationViewModel) และ CLI (BomApp.Cli)
/// </summary>
public interface ICalculateSalesProductionUseCase
{
    /// <summary>
    /// คำนวณวัตถุดิบจากรายการขายตามช่วงวันที่
    /// DryRun = true → คำนวณแต่ไม่ write DB
    /// </summary>
    Task<Result<ProductionResultDto>> CalculateAsync(
        CalculateSalesProductionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// บันทึกเอกสาร bom_production จากผลการคำนวณ
    /// ต้องเรียก CalculateAsync ก่อนเสมอ
    /// </summary>
    Task<Result<IReadOnlyList<BomProductionDto>>> SaveAsync(
        CalculateSalesProductionRequest request,
        CancellationToken ct = default);
}
