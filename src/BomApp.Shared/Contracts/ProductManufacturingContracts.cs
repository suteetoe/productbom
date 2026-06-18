namespace BomApp.Shared.Contracts;

public record ProductManufacturingDto(
    string DocNo,
    DateOnly DocDate,
    string WhCode,
    string ShelfCode,
    string Remark,
    decimal TotalCost,
    IReadOnlyList<ProductManufacturingFinishGoodDto> FinishGoods,
    IReadOnlyList<ProductManufacturingMaterialDto> Materials
);

public record ProductManufacturingListQuery(
    DateOnly? DocDateFrom,
    DateOnly? DocDateTo,
    string? DocNo,
    string? ItemCode,
    int PageNumber,
    int PageSize
);

public record ProductManufacturingFinishGoodDto(
    string DocNo,
    string ItemCode,
    string ItemName,
    decimal Qty,
    string UnitCode,
    string WhCode,
    string ShelfCode,
    decimal CostPerUnit,
    decimal TotalCost,
    int LineNumber
);

public record ProductManufacturingMaterialDto(
    string DocNo,
    string ItemCode,
    string ItemName,
    decimal Qty,
    string UnitCode,
    string WhCode,
    string ShelfCode,
    decimal CostPerUnit,
    decimal TotalCost,
    int LineNumber
);

public record CalculateProductManufacturingRequest(
    DateOnly DocDate,
    string DocNo,
    string WhCode,
    string ShelfCode,
    string Remark,
    IReadOnlyList<CreateProductManufacturingFinishGoodCommand> FinishGoods,
    bool DryRun
);

public record CreateProductManufacturingCommand(
    string DocNo,
    DateOnly DocDate,
    string WhCode,
    string ShelfCode,
    string Remark,
    IReadOnlyList<CreateProductManufacturingFinishGoodCommand> FinishGoods,
    IReadOnlyList<CreateProductManufacturingMaterialCommand> Materials
);

public record UpdateProductManufacturingCommand(
    DateOnly DocDate,
    string WhCode,
    string ShelfCode,
    string Remark,
    IReadOnlyList<CreateProductManufacturingFinishGoodCommand> FinishGoods,
    IReadOnlyList<CreateProductManufacturingMaterialCommand> Materials
);

public record CreateProductManufacturingFinishGoodCommand(
    string ItemCode,
    decimal Qty,
    string UnitCode,
    string WhCode,
    string ShelfCode,
    decimal CostPerUnit,
    decimal TotalCost,
    int LineNumber
);

public record CreateProductManufacturingMaterialCommand(
    string ItemCode,
    string ItemName,
    decimal Qty,
    string UnitCode,
    string WhCode,
    string ShelfCode,
    decimal CostPerUnit,
    decimal TotalCost,
    int LineNumber
);
