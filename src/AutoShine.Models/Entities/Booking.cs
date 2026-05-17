using AutoShine.Models.Common;
using AutoShine.Models.Enums;

namespace AutoShine.Models.Entities;

public class Booking : BaseEntity
{
    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;

    public int? EmployeeId { get; set; }
    public User? Employee { get; set; }

    public int PackageId { get; set; }
    public Package Package { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string? Notes { get; set; }

    // Navigation
    public Review? Review { get; set; }
}
