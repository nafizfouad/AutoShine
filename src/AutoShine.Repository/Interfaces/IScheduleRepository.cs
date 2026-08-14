using AutoShine.Models.Entities;

namespace AutoShine.Repository.Interfaces;

public interface IScheduleRepository
{
    // Templates
    Task<IEnumerable<EmployeeScheduleTemplate>> GetTemplatesByEmployeeAsync(int employeeId);
    Task<IEnumerable<EmployeeScheduleTemplate>> GetActiveTemplatesForDateAsync(DateTime date);
    Task<EmployeeScheduleTemplate?> GetTemplateByIdAsync(int id);
    Task AddTemplateAsync(EmployeeScheduleTemplate template);
    void UpdateTemplate(EmployeeScheduleTemplate template);
    void RemoveTemplate(EmployeeScheduleTemplate template);

    // Leaves
    Task<IEnumerable<EmployeeLeave>> GetLeavesByEmployeeAsync(int employeeId);
    Task<bool> IsOnLeaveAsync(int employeeId, DateTime date);
    Task AddLeaveAsync(EmployeeLeave leave);
    void RemoveLeave(EmployeeLeave leave);
    Task<EmployeeLeave?> GetLeaveByIdAsync(int id);
    Task<IEnumerable<EmployeeLeave>> GetAllLeavesAsync();
}
