using AutoShine.Service.DTOs.Schedules;
using AutoShine.Service.DTOs.Users;

namespace AutoShine.Service.Interfaces;

public interface IScheduleService
{
    // Templates
    Task<IEnumerable<ScheduleTemplateDto>> GetTemplatesByEmployeeAsync(int employeeId);
    Task<IEnumerable<ScheduleTemplateDto>> GetAllTemplatesAsync();
    Task<ScheduleTemplateDto> CreateTemplateAsync(CreateScheduleTemplateDto dto);
    Task<bool> DeleteTemplateAsync(int id);

    // Leaves
    Task<IEnumerable<LeaveDto>> GetAllLeavesAsync();
    Task<IEnumerable<LeaveDto>> GetLeavesByEmployeeAsync(int employeeId);
    Task<LeaveDto> CreateLeaveAsync(CreateLeaveDto dto);
    Task<bool> DeleteLeaveAsync(int id);
}

public interface IProfileService
{
    Task<UserDto?> GetProfileAsync(int userId);
    Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
