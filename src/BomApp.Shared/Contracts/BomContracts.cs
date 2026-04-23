namespace BomApp.Shared.Contracts;

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
