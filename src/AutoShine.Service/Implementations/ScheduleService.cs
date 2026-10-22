using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.Common;
using AutoShine.Service.DTOs.Schedules;
using AutoShine.Service.DTOs.Users;
using AutoShine.Service.Interfaces;
using MapsterMapper;

namespace AutoShine.Service.Implementations;

public class ScheduleService : IScheduleService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    public ScheduleService(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<IEnumerable<ScheduleTemplateDto>> GetTemplatesByEmployeeAsync(int employeeId)
    {
        var templates = await _uow.Schedules.GetTemplatesByEmployeeAsync(employeeId);
        return templates.Select(ToTemplateDto);
    }

    public async Task<IEnumerable<ScheduleTemplateDto>> GetAllTemplatesAsync()
    {
        var employees = await _uow.Users.GetByRoleAsync(UserRole.Employee);
        var all = new List<ScheduleTemplateDto>();
        foreach (var emp in employees)
            all.AddRange((await _uow.Schedules.GetTemplatesByEmployeeAsync(emp.Id)).Select(ToTemplateDto));
        return all;
    }

    public async Task<ScheduleTemplateDto> CreateTemplateAsync(CreateScheduleTemplateDto dto)
    {
        await EnsureEmployeeAsync(dto.EmployeeId);
        ValidateTemplate(dto);

        var template = new EmployeeScheduleTemplate
        {
            EmployeeId = dto.EmployeeId,
            StartDate = DateTimeUtil.AsUtcDate(dto.StartDate),
            EndDate = DateTimeUtil.AsUtcDate(dto.EndDate),
            WorkingDays = dto.WorkingDays,
            WorkStartTime = dto.WorkStartTime,
            WorkEndTime = dto.WorkEndTime,
            BreakStartTime = dto.BreakStartTime,
            BreakEndTime = dto.BreakEndTime,
            IsActive = true
        };
        await _uow.Schedules.AddTemplateAsync(template);
        await _uow.SaveChangesAsync();
        var saved = await _uow.Schedules.GetTemplateByIdAsync(template.Id);
        return ToTemplateDto(saved!);
    }

    public async Task<ScheduleTemplateDto?> UpdateTemplateAsync(int id, CreateScheduleTemplateDto dto)
    {
        var t = await _uow.Schedules.GetTemplateByIdAsync(id);
        if (t == null) return null;

        await EnsureEmployeeAsync(dto.EmployeeId);
        ValidateTemplate(dto);

        t.EmployeeId = dto.EmployeeId;
        t.StartDate = DateTimeUtil.AsUtcDate(dto.StartDate);
        t.EndDate = DateTimeUtil.AsUtcDate(dto.EndDate);
        t.WorkingDays = dto.WorkingDays;
        t.WorkStartTime = dto.WorkStartTime;
        t.WorkEndTime = dto.WorkEndTime;
        t.BreakStartTime = dto.BreakStartTime;
        t.BreakEndTime = dto.BreakEndTime;
        await _uow.SaveChangesAsync();

        var saved = await _uow.Schedules.GetTemplateByIdAsync(t.Id);
        return ToTemplateDto(saved!);
    }

    public async Task<bool> DeleteTemplateAsync(int id)
    {
        var t = await _uow.Schedules.GetTemplateByIdAsync(id);
        if (t == null) return false;
        _uow.Schedules.RemoveTemplate(t);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<LeaveDto>> GetAllLeavesAsync()
        => (await _uow.Schedules.GetAllLeavesAsync()).Select(ToLeaveDto);

    public async Task<IEnumerable<LeaveDto>> GetLeavesByEmployeeAsync(int employeeId)
        => (await _uow.Schedules.GetLeavesByEmployeeAsync(employeeId)).Select(ToLeaveDto);

    public async Task<LeaveDto> CreateLeaveAsync(CreateLeaveDto dto)
    {
        await EnsureEmployeeAsync(dto.EmployeeId);

        var date = DateTimeUtil.AsUtcDate(dto.Date);
        if (await _uow.Schedules.IsOnLeaveAsync(dto.EmployeeId, date))
            throw new InvalidOperationException("This employee already has leave recorded on that date.");

        var reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim();
        if (reason?.Length > 500)
            throw new ArgumentException("Reason must be at most 500 characters.");

        var leave = new EmployeeLeave
        {
            EmployeeId = dto.EmployeeId,
            Date = date,
            Reason = reason
        };
        await _uow.Schedules.AddLeaveAsync(leave);
        await _uow.SaveChangesAsync();
        var saved = await _uow.Schedules.GetLeaveByIdAsync(leave.Id);
        return ToLeaveDto(saved!);
    }

    public async Task<bool> DeleteLeaveAsync(int id)
    {
        var l = await _uow.Schedules.GetLeaveByIdAsync(id);
        if (l == null) return false;
        _uow.Schedules.RemoveLeave(l);
        await _uow.SaveChangesAsync();
        return true;
    }

    private async Task EnsureEmployeeAsync(int employeeId)
    {
        var user = await _uow.Users.GetByIdAsync(employeeId);
        if (user == null || user.Role != UserRole.Employee)
            throw new ArgumentException("Selected user is not an employee.");
    }

    private static void ValidateTemplate(CreateScheduleTemplateDto dto)
    {
        if (dto.EndDate.Date < dto.StartDate.Date)
            throw new ArgumentException("End date must be on or after the start date.");
        if (dto.WorkingDays is < 1 or > 127)
            throw new ArgumentException("Select at least one working day.");
        if (dto.WorkStartTime < TimeSpan.Zero || dto.WorkEndTime > TimeSpan.FromHours(24) || dto.WorkStartTime >= dto.WorkEndTime)
            throw new ArgumentException("Work start time must be before work end time.");

        if (dto.BreakStartTime.HasValue != dto.BreakEndTime.HasValue)
            throw new ArgumentException("Provide both break start and break end, or neither.");
        if (dto.BreakStartTime is TimeSpan bs && dto.BreakEndTime is TimeSpan be
            && (bs >= be || bs < dto.WorkStartTime || be > dto.WorkEndTime))
            throw new ArgumentException("The break must fall inside working hours and start before it ends.");
    }

    private static ScheduleTemplateDto ToTemplateDto(EmployeeScheduleTemplate t) => new(
        t.Id, t.EmployeeId,
        $"{t.Employee.FirstName} {t.Employee.LastName}",
        t.StartDate, t.EndDate, t.WorkingDays,
        t.WorkStartTime, t.WorkEndTime,
        t.BreakStartTime, t.BreakEndTime, t.IsActive);

    private static LeaveDto ToLeaveDto(EmployeeLeave l) => new(
        l.Id, l.EmployeeId,
        $"{l.Employee.FirstName} {l.Employee.LastName}",
        l.Date, l.Reason);
}

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _uow;
    public ProfileService(IUnitOfWork uow) => _uow = uow;

    public async Task<UserDto?> GetProfileAsync(int userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) return null;
        return ToDto(user);
    }

    public async Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) return null;
        user.FirstName = Validate.Name(dto.FirstName, "First name");
        user.LastName = Validate.Name(dto.LastName, "Last name");
        user.Phone = Validate.Phone(dto.Phone, required: false);
        await _uow.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) return false;
        if (string.IsNullOrEmpty(dto.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");
        Validate.Password(dto.NewPassword);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _uow.SaveChangesAsync();
        return true;
    }

    private static UserDto ToDto(User user) => new(user.Id, user.FirstName, user.LastName,
        user.Email, user.Phone, user.Role.ToString(), user.IsActive);
}
