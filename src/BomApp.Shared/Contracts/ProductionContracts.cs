namespace BomApp.Shared.Contracts;

/// <summary>Production Order header — ใช้แสดงใน Production List (2.4)</summary>
public record ProductionOrderDto(
    Guid          Id,
    string        OrderNo,            // PO-YYYYMM-NNNNN
    Guid          BomId,
    string        BomCode,
    string        ItemCode,           // สินค้าที่ผลิต
    string        ItemName,
    decimal       Quantity,
    string        Status,             // Pending | Processing | Done | Cancelled
    string[]      SourceSoNumbers,    // doc_no จาก ic_trans_detail
    DateOnly?     SourceDocDateFrom,
    DateOnly?     SourceDocDateTo,
    string        CreatedBy,          // username หรือ "SYSTEM"
    string        CreatedVia,         // "UI" | "CLI"
    DateTime      CreatedAt,
    string?       Notes
);

/// <summary>Production Order line — วัตถุดิบที่ต้องเบิก</summary>
public record ProductionOrderLineDto(
    Guid     Id,
    string   MaterialCode,
    string   MaterialName,
    decimal  RequiredQuantity,
    string   Unit
);

/// <summary>Production calculation document grouped from selected sales items.</summary>
public record BomProductionDto(
    Guid     Id,
    DateOnly DocDate,
    string   DocNo,
    TimeOnly DocTime,
    IReadOnlyList<BomProductionOrderDto> Orders,
    IReadOnlyList<BomProductionDetailDto> Details
);

/// <summary>Selected sales item stored in bom_production_orders.</summary>
public record BomProductionOrderDto(
    Guid    Id,
    string  DocNo,
    DateOnly DocDate,
    string  RefDocNo,
    DateOnly RefDocDate,
    string  ItemCode,
    decimal Qty,
    string  UnitCode
);

/// <summary>Material/item requirement stored in bom_production_details.</summary>
public record BomProductionDetailDto(
    Guid    Id,
    string  DocNo,
    string  ItemCode,
    string  ItemName,
    decimal Qty,
    string  UnitCode
);

/// <summary>ผลลัพธ์การคำนวณ — แสดงใน 2.5 ก่อนบันทึก</summary>
public record ProductionResultDto(
    IReadOnlyList<ProductionResultItemDto> Items,      // สรุปต่อสินค้า
    IReadOnlyList<MaterialRequirementDto>  Materials,  // สรุปวัตถุดิบรวม
    int    SkippedItemCount,   // จำนวน item ที่ไม่มี BOM (ข้ามไป)
    string[]  SkippedItemCodes // รหัสที่ข้ามไป
);

public record ProductionResultItemDto(
    string   ItemCode,
    string   ItemName,
    string   BomCode,
    decimal  SaleQty,
    string   SaleUnit,
    decimal  QtyInBaseUnit,   // หลังแปลงหน่วยแล้ว
    IReadOnlyList<MaterialRequirementDto> Materials
);

public record MaterialRequirementDto(
    string   MaterialCode,
    string   MaterialName,
    decimal  RequiredQty,
    string   Unit
);

/// <summary>Request สำหรับ CalculateSalesProductionUseCase (ทั้ง UI และ CLI)</summary>
public record CalculateSalesProductionRequest(
    DateOnly        DateFrom,
    DateOnly        DateTo,
    SaveMode        Mode,       // Daily | PerDocument
    bool            DryRun,     // true = คำนวณแต่ไม่ write DB
    string          CreatedBy,  // username หรือ "SYSTEM"
    string          CreatedVia  // "UI" | "CLI"
);

public enum SaveMode { Daily, PerDocument }

/// <summary>สำหรับ Cancel production order</summary>
public record CancelProductionOrderCommand(Guid OrderId, string Reason);

/// <summary>Internal command สำหรับสร้าง production order ใน repository</summary>
public record CreateProductionOrderInternalCommand(
    Guid BomId,
    string BomCode,
    string ItemCode,
    string ItemName,
    decimal Quantity,
    string BomSnapshot,         // JSON string — snapshot of BOM at creation time
    string[] SourceSoNumbers,
    DateOnly? SourceDocDateFrom,
    DateOnly? SourceDocDateTo,
    string CreatedBy,
    string CreatedVia,
    string? Notes
);

/// <summary>Internal command for creating selected sales rows in bom_production_orders.</summary>
public record CreateBomProductionInternalCommand(
    DateOnly DocDate,
    TimeOnly DocTime,
    IReadOnlyList<CreateBomProductionOrderInternalCommand> Orders,
    IReadOnlyList<CreateBomProductionDetailInternalCommand> Details
);

public record CreateBomProductionOrderInternalCommand(
    string RefDocNo,
    DateOnly RefDocDate,
    string ItemCode,
    decimal Qty,
    string UnitCode
);

public record CreateBomProductionDetailInternalCommand(
    string ItemCode,
    string ItemName,
    decimal Qty,
    string UnitCode
);
