using AutoShine.Models.Common;

namespace AutoShine.Models.Entities;

public class Package : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<PackageItem> PackageItems { get; set; } = new List<PackageItem>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
