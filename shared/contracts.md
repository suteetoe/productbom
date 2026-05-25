# Shared Contracts — DTOs
> ไฟล์นี้ CTO approve เท่านั้น — ห้าม agent ไหน modify โดยไม่ผ่าน CTO
> Interface definitions แยกอยู่ที่ `shared/interfaces.md`
> Last updated: 2026-04-17

---

## BOM Domain DTOs (`BomApp.Shared`)

```csharp
/// <summary>BOM header + lines — ใช้ส่งระหว่าง Application → UI</summary>
public record BomDto(
    Guid          Id,
    string        Code,
    string        Name,
    string?       Description,
    string        ItemCode,           // สินค้าที่ใช้สูตรนี้ (ref ic_inventory.code)
    string        ItemName,           // ชื่อสินค้า (denormalized)
    decimal       YieldQuantity,      // จำนวนที่ผลิตได้ต่อ 1 รอบ
    string        YieldUnit,          // หน่วยนับผลผลิต (ref ic_unit.code)
    int           Version,
    string        Status,             // Draft | Active | Inactive
    DateTime      CreatedAt,
    DateTime      UpdatedAt,
    string        CreatedBy,
    IReadOnlyList<BomLineDto> Lines
);

public record BomLineDto(
    Guid     Id,
    string   MaterialCode,            // ref ic_inventory.code
    string   MaterialName,            // denormalized
    decimal  Quantity,
    string   Unit,                    // ref ic_unit.code
    Guid?    SubBomId,                // ถ้าเป็น sub-assembly
    int      SortOrder,
    string?  Notes
);

/// <summary>สำหรับสร้าง BOM ใหม่</summary>
public record CreateBomCommand(
    string        Code,
    string        Name,
    string?       Description,
    string        ItemCode,
    decimal       YieldQuantity,
    string        YieldUnit,
    IReadOnlyList<CreateBomLineCommand> Lines
);

public record CreateBomLineCommand(
    string   MaterialCode,
    decimal  Quantity,
    string   Unit,
    Guid?    SubBomId,
    int      SortOrder,
    string?  Notes
);

/// <summary>สำหรับแก้ไข BOM</summary>
public record UpdateBomCommand(
    string?       Name,
    string?       Description,
    string?       ItemCode,
    decimal?      YieldQuantity,
    string?       YieldUnit,
    IReadOnlyList<CreateBomLineCommand>? Lines
);
```

---

## Production Domain DTOs

```csharp
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

/// <summary>Production issue document header — เอกสารเบิกรายการสินค้าที่ผลิต</summary>
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

/// <summary>Internal command สำหรับสร้าง bom_productions + bom_production_orders + bom_production_details</summary>
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
```

---

## Auth DTO

```csharp
/// <summary>ผลลัพธ์จาก login — ใช้สร้าง session</summary>
public record AuthUserDto(
    string  UserCode,
    string  UserName,
    short   UserLevel
);
```

---

## ERP DTOs (`BomApp.Infrastructure` — read-only จาก ERP)

```csharp
/// <summary>สินค้า / วัตถุดิบจาก ic_inventory</summary>
public record ErpItemDto(
    string  Code,      // ic_inventory.code
    string  Name,      // ic_inventory.name_1
    string  UnitCost   // ic_inventory.unit_cost
);

/// <summary>หน่วยนับต่อสินค้าจาก ic_unit_use + ic_unit</summary>
public record ErpUnitDto(
    string   Code,          // ic_unit.code
    string   Name,          // ic_unit.name_1
    string   IcCode,        // ic_unit_use.ic_code
    decimal  StandValue,    // ic_unit_use.stand_value
    decimal  DivideValue,   // ic_unit_use.divide_value
    int      Ratio,         // ic_unit_use.ratio
    int      LineNumber     // ic_unit_use.line_number
)
{
    /// <summary>จำนวนในหน่วยหลัก = qty × StandValue / DivideValue</summary>
    public decimal ToBaseUnit(decimal qty) =>
        DivideValue == 0 ? qty : qty * StandValue / DivideValue;
}

/// <summary>รายการขายจาก ic_trans_detail (trans_flag=44, last_status=0)</summary>
public record ErpSalesTransactionDto(
    DateOnly  DocDate,
    string    DocNo,
    string    ItemCode,
    decimal   Qty,
    string    UnitCode,
    decimal   StandValue,
    decimal   DivideValue
)
{
    /// <summary>จำนวนในหน่วยหลัก สำหรับคำนวณ BOM</summary>
    public decimal QtyInBaseUnit =>
        DivideValue == 0 ? Qty : Qty * StandValue / DivideValue;
}
```

---

## Change Log

| วันที่ | เปลี่ยนอะไร | ผลกระทบ | Approved by |
|---|---|---|---|
| Sprint 1 | Initial draft | — | — |
| 2026-04-17 | อัปเดต BomDto เพิ่ม ItemCode, YieldQuantity, YieldUnit | team-a ต้อง update migration | CTO |
| 2026-04-17 | อัปเดต ProductionOrderDto เพิ่ม SourceSoNumbers, CreatedVia | team-b ต้อง update 2.4 DataGrid | CTO |
| 2026-04-17 | เพิ่ม CalculateSalesProductionRequest, SaveMode | team-a + CLI project | CTO |
| 2026-04-17 | เพิ่ม ErpSalesTransactionDto, ErpUnitDto, AuthUserDto | team-c + team-a | CTO |
| 2026-04-17 | แยก Interface ออกไป shared/interfaces.md | ทุกทีมอ่าน interfaces.md แทน | CTO |
