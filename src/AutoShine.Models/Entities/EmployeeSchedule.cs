using AutoShine.Models.Common;

namespace AutoShine.Models.Entities;

/// <summary>
/// Defines an employee's working availability window for a specific day/time block.
/// </summary>
public class EmployeeSchedule : BaseEntity
{
    public int EmployeeId { get; set; }
    public User Employee { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
}
