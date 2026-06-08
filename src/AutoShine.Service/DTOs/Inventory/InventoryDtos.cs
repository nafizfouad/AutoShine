namespace AutoShine.Service.DTOs.Inventory;

public record InventoryItemDto(
    int Id,
    string ItemName,
    string SKU,
    int CurrentStock,
    int MinimumThreshold,
    string Unit,
    bool IsLowStock
);

public record CreateInventoryItemDto(
    string ItemName,
    string SKU,
    int CurrentStock,
    int MinimumThreshold,
    string Unit
);

public record UpdateInventoryItemDto(
    string ItemName,
    int CurrentStock,
    int MinimumThreshold,
    string Unit
);
