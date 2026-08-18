using AutoShine.Models.Entities;
using AutoShine.Repository.Interfaces;
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
        var employees = await _uow.Users.GetByRoleAsync(AutoShine.Models.Enums.UserRole.Employee);
        var all = new List<ScheduleTemplateDto>();
        foreach (var emp in employees)
            all.AddRange((await _uow.Schedules.GetTemplatesByEmployeeAsync(emp.Id)).Select(ToTemplateDto));
        return all;
    }

    public async Task<ScheduleTemplateDto> CreateTemplateAsync(CreateScheduleTemplateDto dto)
    {
        var template = new EmployeeScheduleTemplate
        {
            EmployeeId = dto.EmployeeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            WorkingDays = dto.WorkingDays,
            WorkStartTime = dto.WorkStartTime,
            WorkEndTime = dto.WorkEndTime,
            BreakStartTime = dto.BreakStartTime,
            BreakEndTime = dto.BreakEndTime,
            IsActive = true
        };
        await _uow.Schedules.AddTemplateAsync(template);
        await _uow.SaveChangesAsync();
        var saved = (await _uow.Schedules.GetTemplatesByEmployeeAsync(dto.EmployeeId))
            .First(t => t.Id == template.Id);
        return ToTemplateDto(saved);
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
        var leave = new EmployeeLeave
        {
            EmployeeId = dto.EmployeeId,
            Date = dto.Date.Date,
            Reason = dto.Reason
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
        return new UserDto(user.Id, user.FirstName, user.LastName,
            user.Email, user.Phone, user.Role.ToString(), user.IsActive);
    }

    public async Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) return null;
        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Phone = dto.Phone?.Trim();
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
        return new UserDto(user.Id, user.FirstName, user.LastName,
            user.Email, user.Phone, user.Role.ToString(), user.IsActive);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) return false;
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");
        if (dto.NewPassword.Length < 8)
            throw new ArgumentException("New password must be at least 8 characters.");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
        return true;
    }
}
