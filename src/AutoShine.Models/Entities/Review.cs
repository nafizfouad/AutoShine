using AutoShine.Models.Common;

namespace AutoShine.Models.Entities;

public class Review : BaseEntity
{
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;

    public int EmployeeId { get; set; }
    public User Employee { get; set; } = null!;

    public int Rating { get; set; } // 1–5
    public string? Comment { get; set; }
}
