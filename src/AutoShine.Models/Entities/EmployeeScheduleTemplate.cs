using AutoShine.Models.Common;

namespace AutoShine.Models.Entities;

/// <summary>
/// Defines recurring work availability for an employee over a date range.
/// WorkingDays uses a bitmask: 1=Sun, 2=Mon, 4=Tue, 8=Wed, 16=Thu, 32=Fri, 64=Sat
/// </summary>
public class EmployeeScheduleTemplate : BaseEntity
{
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WorkingDays { get; set; }           // bitmask
    public TimeSpan WorkStartTime { get; set; }
    public TimeSpan WorkEndTime { get; set; }
    public TimeSpan? BreakStartTime { get; set; }
    public TimeSpan? BreakEndTime { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual User Employee { get; set; } = null!;

    // Helper: does this template cover the given day of week?
    public bool CoversDay(DayOfWeek day) => (WorkingDays & (1 << (int)day)) != 0;
}
