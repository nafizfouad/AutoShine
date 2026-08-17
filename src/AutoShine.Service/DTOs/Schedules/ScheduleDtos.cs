namespace AutoShine.Service.DTOs.Schedules;

// ── Template DTOs ──────────────────────────────────────────────────────────

public record CreateScheduleTemplateDto(
    int EmployeeId,
    DateTime StartDate,
    DateTime EndDate,
    int WorkingDays,          // bitmask: 1=Sun,2=Mon,4=Tue,8=Wed,16=Thu,32=Fri,64=Sat
    TimeSpan WorkStartTime,
    TimeSpan WorkEndTime,
    TimeSpan? BreakStartTime,
    TimeSpan? BreakEndTime
);

public record ScheduleTemplateDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    DateTime StartDate,
    DateTime EndDate,
    int WorkingDays,
    TimeSpan WorkStartTime,
    TimeSpan WorkEndTime,
    TimeSpan? BreakStartTime,
    TimeSpan? BreakEndTime,
    bool IsActive
);

// ── Leave DTOs ──────────────────────────────────────────────────────────────

public record CreateLeaveDto(
    int EmployeeId,
    DateTime Date,
    string? Reason
);

public record LeaveDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    DateTime Date,
    string? Reason
);

// ── Profile DTOs ─────────────────────────────────────────────────────────────

public record UpdateProfileDto(
    string FirstName,
    string LastName,
    string? Phone
);

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);
