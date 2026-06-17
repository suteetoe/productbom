# Shared Interfaces
> ไฟล์นี้ CTO approve เท่านั้น — ห้าม agent ไหน modify โดยไม่ผ่าน CTO
> DTOs อยู่ที่ `shared/contracts.md`
> Last updated: 2026-06-17

---

## File Ownership

| Interface | Layer | เจ้าของ | หมายเหตุ |
|---|---|---|---|
| `IBomRepository` | Infrastructure | team-a-backend | |
| `IBomAssignmentRepository` | Infrastructure | team-a-backend | |
| `IProductionOrderRepository` | Infrastructure | team-a-backend | |
| `IBomService` | Application | team-a-backend | |
| `IProductionService` | Application | team-a-backend | |
| `ICalculateSalesProductionUseCase` | Application | team-a-backend | ใช้ทั้ง UI และ CLI |
| `IAuthRepository` | Infrastructure | team-a-backend | อ่านจาก authentication-database |
| `IErpItemRepository` | Infrastructure | team-c-integration | อ่านจาก erp-database |
| `IErpSalesOrderRepository` | Infrastructure | team-c-integration | อ่านจาก erp-database |
| `IErpStockRequestProcessor` | Infrastructure | team-c-integration | POST ERP process stock request หลังบันทึกเอกสารผลิต |

---

## Application Layer Interfaces (`BomApp.Application`)

### `IBomService`

```csharp
public interface IBomService
{
    Task<Result<IReadOnlyList<BomDto>>> GetAllAsync(
        CancellationToken ct = default);

    Task<Result<BomDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Result<BomDto>> GetByCodeAsync(
        string code,
        CancellationToken ct = default);

    Task<Result<BomDto>> CreateAsync(
        CreateBomCommand cmd,
        CancellationToken ct = default);

    Task<Result<BomDto>> UpdateAsync(
        Guid id,
        UpdateBomCommand cmd,
        CancellationToken ct = default);

    Task<Result> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

---

### `IProductionService`

```csharp
public interface IProductionService
{
    Task<Result<IReadOnlyList<ProductionOrderDto>>> GetOrdersAsync(
        DateOnly?    dateFrom    = null,
        DateOnly?    dateTo      = null,
        string?      status      = null,
        string?      itemCode    = null,
        string?      createdVia  = null,   // "UI" | "CLI" | null = ทั้งหมด
        string?      sourceDocNo = null,   // ค้นหาใน source_so_numbers[]
        CancellationToken ct = default);

    Task<Result<ProductionOrderDto>> GetOrderByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<ProductionOrderLineDto>>> GetOrderLinesAsync(
        Guid productionOrderId,
        CancellationToken ct = default);

    Task<Result> CancelOrderAsync(
        CancelProductionOrderCommand cmd,
        CancellationToken ct = default);
}
```

---

### `IProductDestructionService`

```csharp
public interface IProductDestructionService
{
    Task<Result<PagedResult<ProductDestructionDto>>> GetDocumentsPageAsync(
        ProductDestructionListQuery query,
        CancellationToken ct = default);

    Task<Result<ProductDestructionDto>> GetDocumentByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<Result<ProductDestructionDto>> CreateAsync(
        CreateProductDestructionCommand command,
        CancellationToken ct = default);

    Task<Result<ProductDestructionDto>> UpdateAsync(
        string docNo,
        UpdateProductDestructionCommand command,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        string docNo,
        CancellationToken ct = default);
}
```

---

### `ICalculateSalesProductionUseCase`

> Use case หลักสำหรับหน้าจอ 2.5 — เรียกได้ทั้งจาก UI และ CLI
> ดู ADR `shared/adr/001-cli-command-for-sales-calculation.md`

```csharp
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
    /// บันทึก bom_productions + bom_production_orders + bom_production_details จากผลการคำนวณ
    /// ต้องเรียก CalculateAsync ก่อนเสมอ
    /// </summary>
    Task<Result<IReadOnlyList<BomProductionDto>>> SaveAsync(
        CalculateSalesProductionRequest request,
        CancellationToken ct = default);
}
```

---

## Infrastructure Layer — BOM Database (`BomApp.Infrastructure`)

### `IBomRepository`

```csharp
public interface IBomRepository
{
    Task<IReadOnlyList<BomDto>> GetAllAsync(CancellationToken ct = default);
    Task<BomDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BomDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default);
    Task<BomDto> CreateAsync(CreateBomCommand cmd, string createdBy, CancellationToken ct = default);
    Task<BomDto> UpdateAsync(Guid id, UpdateBomCommand cmd, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SetStatusAsync(Guid id, string status, CancellationToken ct = default);
}
```

---

### `IBomAssignmentRepository`

```csharp
public interface IBomAssignmentRepository
{
    Task<Guid?> GetBomIdByItemCodeAsync(string itemCode, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, Guid>> GetAssignedItemCodesAsync(
        IReadOnlyList<string> itemCodes,
        CancellationToken ct = default);
    Task AssignAsync(string itemCode, string itemName, Guid bomId, string assignedBy, CancellationToken ct = default);
    Task RemoveAsync(string itemCode, CancellationToken ct = default);
}
```

---

### `IProductionOrderRepository`

```csharp
public interface IProductionOrderRepository
{
    Task<IReadOnlyList<ProductionOrderDto>> GetAllAsync(
        DateOnly?    dateFrom    = null,
        DateOnly?    dateTo      = null,
        string?      status      = null,
        string?      itemCode    = null,
        string?      createdVia  = null,
        string?      sourceDocNo = null,
        CancellationToken ct = default);

    Task<ProductionOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ProductionOrderLineDto>> GetLinesByOrderIdAsync(
        Guid productionOrderId,
        CancellationToken ct = default);

    /// <summary>ตรวจว่า doc_no เหล่านี้มีใน source_so_numbers ของ order ที่มีอยู่แล้วหรือไม่</summary>
    Task<IReadOnlyList<string>> GetAlreadyProcessedDocNosAsync(
        IReadOnlyList<string> docNos,
        CancellationToken ct = default);

    Task<ProductionOrderDto> CreateAsync(
        CreateProductionOrderInternalCommand cmd,
        CancellationToken ct = default);

    Task SetStatusAsync(Guid id, string status, CancellationToken ct = default);
}
```

---

### `IBomProductionRepository`

```csharp
public interface IBomProductionRepository
{
    Task<IReadOnlyList<BomProductionDto>> GetAllAsync(
        DateOnly? docDateFrom = null,
        DateOnly? docDateTo = null,
        string? docNo = null,
        string? itemCode = null,
        CancellationToken ct = default);

    Task<BomProductionDto?> GetByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<IReadOnlyList<BomProductionOrderDto>> GetOrdersByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<IReadOnlyList<BomProductionDetailDto>> GetDetailsByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<bool> DeleteByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<BomProductionDto> CreateAsync(
        CreateBomProductionInternalCommand cmd,
        CancellationToken ct = default);
}
```

---

## Infrastructure Layer — Authentication Database

### `IProductDestructionRepository`

```csharp
public interface IProductDestructionRepository
{
    Task<PagedResult<ProductDestructionDto>> GetPageAsync(
        ProductDestructionListQuery query,
        CancellationToken ct = default);

    Task<ProductDestructionDto?> GetByDocNoAsync(
        string docNo,
        CancellationToken ct = default);

    Task<ProductDestructionDto> CreateAsync(
        CreateProductDestructionCommand command,
        CancellationToken ct = default);

    Task<ProductDestructionDto?> UpdateAsync(
        string docNo,
        UpdateProductDestructionCommand command,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(
        string docNo,
        CancellationToken ct = default);
}
```

---

### `IAuthRepository`

> Connection: `authentication-database` — อ่านอย่างเดียว
> ดู `shared/auth-spec.md` สำหรับ `sml_user_list` schema

```csharp
public interface IAuthRepository
{
    /// <summary>
    /// ตรวจสอบ user จาก sml_user_list
    /// WHERE user_code = @code AND user_password = @password
    ///   AND active_status = 1 AND is_lock_record = 0
    /// คืนค่า null ถ้าไม่ผ่าน
    /// </summary>
    Task<AuthUserDto?> ValidateUserAsync(
        string userCode,
        string password,
        CancellationToken ct = default);
}
```

---

## Infrastructure Layer — ERP Database (team-c-integration)

> Connection: `erp-database` — อ่านอย่างเดียว
> ดู `shared/erp-spec.md` สำหรับ schema ละเอียด

### `IErpItemRepository`

```csharp
public interface IErpItemRepository
{
    /// <summary>ดึงสินค้าทั้งหมดจาก ic_inventory</summary>
    Task<IReadOnlyList<ErpItemDto>> GetAllItemsAsync(
        CancellationToken ct = default);

    /// <summary>ค้นหาสินค้าตาม code หรือ name_1</summary>
    Task<IReadOnlyList<ErpItemDto>> SearchItemsAsync(
        string keyword,
        CancellationToken ct = default);

    Task<ErpItemDto?> GetItemByCodeAsync(
        string code,
        CancellationToken ct = default);

    /// <summary>ดึงหน่วยนับทั้งหมดของสินค้าจาก ic_unit_use JOIN ic_unit</summary>
    Task<IReadOnlyList<ErpUnitDto>> GetUnitsByItemCodeAsync(
        string icCode,
        CancellationToken ct = default);

    /// <summary>ดึง ic_unit ทั้งหมด (master)</summary>
    Task<IReadOnlyList<ErpUnitDto>> GetAllUnitsAsync(
        CancellationToken ct = default);
}
```

---

### `IErpSalesOrderRepository`

```csharp
public interface IErpSalesOrderRepository
{
    /// <summary>
    /// ดึงรายการขายจาก ic_trans_detail
    /// WHERE trans_flag = 44 AND last_status = 0
    ///   AND doc_date BETWEEN @dateFrom AND @dateTo
    /// </summary>
    Task<IReadOnlyList<ErpSalesTransactionDto>> GetSalesTransactionsByDateRangeAsync(
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken ct = default);
}
```

---

### `IErpStockRequestProcessor`

```csharp
public interface IErpStockRequestProcessor
{
    /// <summary>
    /// ส่งคำสั่งให้ ERP ประมวลผล stock request หลังจากบันทึกเอกสารผลิตเข้า ERP แล้ว
    /// Infrastructure ต้องสร้าง payload: providerCode, databaseName, itemCode[]
    /// </summary>
    Task ProcessStockRequestAsync(
        IReadOnlyList<string> itemCodes,
        CancellationToken ct = default);
}
```

---

### `IErpProductionRepository`

```csharp
public interface IErpProductionRepository
{
    Task SaveProductionDocumentAsync(
        BomProductionDto document,
        CancellationToken ct = default);

    Task SaveProductDestructionDocumentAsync(
        ProductDestructionDto document,
        CancellationToken ct = default);

    Task DeleteProductionDocumentAsync(
        string docNo,
        CancellationToken ct = default);

    Task DeleteProductDestructionDocumentAsync(
        string docNo,
        CancellationToken ct = default);
}
```

---

## Fake Implementations (team-c-integration — สำหรับ unit test)

```csharp
// ที่อยู่: tests/BomApp.Tests.Fakes/
public class FakeErpItemRepository     : IErpItemRepository     { ... }
public class FakeErpSalesOrderRepository : IErpSalesOrderRepository { ... }
```

> Fake ต้องมี seed data ครอบคลุม:
> - สินค้าที่มี BOM assign แล้ว
> - สินค้าที่ยังไม่มี BOM assign (สำหรับ test warning path)
> - รายการขายที่มีหลาย unit (สำหรับ test unit conversion)
> - รายการขายที่ข้ามวัน (สำหรับ test daily consolidation)

---

## Change Log

| วันที่ | เปลี่ยนอะไร | ผลกระทบ |
|---|---|---|
| 2026-04-17 | สร้างไฟล์ใหม่ — แยกออกจาก contracts.md | ทุกทีมอ่านไฟล์นี้สำหรับ interfaces |
| 2026-04-17 | `IErpSalesOrderRepository` — เปลี่ยนเป็น `GetSalesTransactionsByDateRangeAsync` | team-c ต้อง update implementation |
| 2026-04-17 | `IErpItemRepository` — เพิ่ม `GetUnitsByItemCodeAsync`, `SearchItemsAsync` | team-c ต้อง implement เพิ่ม |
| 2026-04-17 | เพิ่ม `ICalculateSalesProductionUseCase` | team-a implement, ทั้ง UI + CLI ใช้ |
| 2026-04-17 | เพิ่ม `IAuthRepository` | team-a implement |
| 2026-04-17 | เพิ่ม `IBomAssignmentRepository`, `IProductionOrderRepository` | team-a implement |
| 2026-06-01 | เพิ่ม `IErpStockRequestProcessor` สำหรับเรียก ERP `processstockrequest` หลัง save production document | team-c implement, team-a เรียกจาก use case |
| 2026-06-17 | Add product destruction delete flow through local repository, ERP delete, and stock request processor | team-a/team-c implement |
