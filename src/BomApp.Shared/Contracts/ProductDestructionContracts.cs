namespace BomApp.Shared.Contracts;

public record ProductDestructionDto(
    string DocNo,
    DateOnly DocDate,
    string WhCode,
    string ShelfCode,
    string Remark,
    IReadOnlyList<ProductDestructionPictureDto> Pictures,
    IReadOnlyList<ProductDestructionDetailDto> Details
);

public record ProductDestructionListQuery(
    DateOnly? DocDateFrom,
    DateOnly? DocDateTo,
    string? DocNo,
    int PageNumber,
    int PageSize
);

public record ProductDestructionPictureDto(
    string DocNo,
    short LineNumber,
    string ImageGuid,
    byte[] ImageFile
);

public record ProductDestructionDetailDto(
    string DocNo,
    string ItemCode,
    string ItemName,
    decimal Qty,
    string UnitCode,
    string WhCode,
    string ShelfCode,
    int LineNumber
);

public record CreateProductDestructionCommand(
    string DocNo,
    DateOnly DocDate,
    string WhCode,
    string ShelfCode,
    string Remark,
    IReadOnlyList<CreateProductDestructionPictureCommand> Pictures,
    IReadOnlyList<CreateProductDestructionDetailCommand> Details
);

public record UpdateProductDestructionCommand(
    DateOnly DocDate,
    string WhCode,
    string ShelfCode,
    string Remark,
    IReadOnlyList<CreateProductDestructionPictureCommand> Pictures,
    IReadOnlyList<CreateProductDestructionDetailCommand> Details
);

public record CreateProductDestructionPictureCommand(
    string ImageGuid,
    byte[] ImageFile,
    short LineNumber
);

public record CreateProductDestructionDetailCommand(
    string ItemCode,
    decimal Qty,
    string UnitCode,
    string WhCode,
    string ShelfCode,
    int LineNumber
);
