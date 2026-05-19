using AutoShine.Models.Common;

namespace AutoShine.Models.Entities;

/// <summary>
/// The "Recipe" mapping table: links a Package to the InventoryItems it consumes.
/// </summary>
public class PackageItem : BaseEntity
{
    public int PackageId { get; set; }
    public Package Package { get; set; } = null!;

    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;

    public int QuantityRequired { get; set; }
}
