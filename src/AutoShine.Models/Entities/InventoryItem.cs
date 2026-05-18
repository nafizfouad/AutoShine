using AutoShine.Models.Common;

namespace AutoShine.Models.Entities;

public class InventoryItem : BaseEntity
{
    public string ItemName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumThreshold { get; set; }
    public string Unit { get; set; } = "units"; // e.g., liters, pieces

    // Navigation
    public ICollection<PackageItem> PackageItems { get; set; } = new List<PackageItem>();
}
