using AutoShine.Models.Common;
using AutoShine.Models.Enums;

namespace AutoShine.Models.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Booking> CustomerBookings { get; set; } = new List<Booking>();
    public ICollection<Booking> EmployeeBookings { get; set; } = new List<Booking>();
    public ICollection<EmployeeSchedule> Schedules { get; set; } = new List<EmployeeSchedule>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
