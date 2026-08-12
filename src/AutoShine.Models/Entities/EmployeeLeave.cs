using AutoShine.Models.Common;

namespace AutoShine.Models.Entities;

/// <summary>
/// Represents a day on which an employee is marked as unavailable (leave/holiday).
/// Admin-managed override that takes priority over schedule templates.
/// </summary>
public class EmployeeLeave : BaseEntity
{
    public int EmployeeId { get; set; }
    public DateTime Date { get; set; }          // date-only (time part ignored)
    public string? Reason { get; set; }

    public virtual User Employee { get; set; } = null!;
}
