namespace AutoShine.Service.DTOs.Packages;

public record PackageDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int EstimatedDurationMinutes,
    bool IsActive,
    List<PackageItemDto> Items
);

public record PackageItemDto(
    int InventoryItemId,
    string ItemName,
    int QuantityRequired,
    string Unit
);

public record CreatePackageDto(
    string Name,
    string Description,
    decimal Price,
    int EstimatedDurationMinutes,
    List<CreatePackageItemDto> Items
);

public record CreatePackageItemDto(
    int InventoryItemId,
    int QuantityRequired
);

public record UpdatePackageDto(
    string Name,
    string Description,
    decimal Price,
    int EstimatedDurationMinutes,
    bool IsActive,
    List<CreatePackageItemDto> Items
);
