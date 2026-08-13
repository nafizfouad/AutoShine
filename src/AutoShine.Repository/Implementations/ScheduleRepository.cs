using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoShine.Repository.Implementations;

public class ScheduleRepository : IScheduleRepository
{
    private readonly AppDbContext _context;
    public ScheduleRepository(AppDbContext context) => _context = context;

    // ── Templates ─────────────────────────────────────────────────────────────
    public async Task<IEnumerable<EmployeeScheduleTemplate>> GetTemplatesByEmployeeAsync(int employeeId)
        => await _context.EmployeeScheduleTemplates
            .Where(t => t.EmployeeId == employeeId && t.IsActive)
            .Include(t => t.Employee)
            .OrderBy(t => t.StartDate)
            .ToListAsync();

    public async Task<IEnumerable<EmployeeScheduleTemplate>> GetActiveTemplatesForDateAsync(DateTime date)
        => await _context.EmployeeScheduleTemplates
            .Where(t => t.IsActive && t.StartDate <= date && t.EndDate >= date)
            .Include(t => t.Employee)
            .ToListAsync();

    public async Task<EmployeeScheduleTemplate?> GetTemplateByIdAsync(int id)
        => await _context.EmployeeScheduleTemplates
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddTemplateAsync(EmployeeScheduleTemplate template)
        => await _context.EmployeeScheduleTemplates.AddAsync(template);

    public void UpdateTemplate(EmployeeScheduleTemplate template)
        => _context.EmployeeScheduleTemplates.Update(template);

    public void RemoveTemplate(EmployeeScheduleTemplate template)
        => _context.EmployeeScheduleTemplates.Remove(template);

    // ── Leaves ────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<EmployeeLeave>> GetLeavesByEmployeeAsync(int employeeId)
        => await _context.EmployeeLeaves
            .Where(l => l.EmployeeId == employeeId)
            .Include(l => l.Employee)
            .OrderBy(l => l.Date)
            .ToListAsync();

    public async Task<bool> IsOnLeaveAsync(int employeeId, DateTime date)
        => await _context.EmployeeLeaves
            .AnyAsync(l => l.EmployeeId == employeeId && l.Date.Date == date.Date);

    public async Task AddLeaveAsync(EmployeeLeave leave)
        => await _context.EmployeeLeaves.AddAsync(leave);

    public void RemoveLeave(EmployeeLeave leave)
        => _context.EmployeeLeaves.Remove(leave);

    public async Task<EmployeeLeave?> GetLeaveByIdAsync(int id)
        => await _context.EmployeeLeaves
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<IEnumerable<EmployeeLeave>> GetAllLeavesAsync()
        => await _context.EmployeeLeaves
            .Include(l => l.Employee)
            .OrderBy(l => l.Date)
            .ToListAsync();
}
