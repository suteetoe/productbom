namespace BomApp.Shared.Contracts;

/// <summary>สินค้า / วัตถุดิบจาก ic_inventory</summary>
public record ErpItemDto(
    string  Code,      // ic_inventory.code
    string  Name,      // ic_inventory.name_1
    string  UnitCost   // ic_inventory.unit_cost
);

public record ErpItemListQuery(
    string? SearchText,
    int PageNumber,
    int PageSize
);

/// <summary>หน่วยนับต่อสินค้าจาก ic_unit_use JOIN ic_unit</summary>
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
    decimal   DivideValue,
    string    WhCode = "",
    string    ShelfCode = ""
)
{
    /// <summary>จำนวนในหน่วยหลัก สำหรับคำนวณ BOM</summary>
    public decimal QtyInBaseUnit =>
        DivideValue == 0 ? Qty : Qty * StandValue / DivideValue;
}
