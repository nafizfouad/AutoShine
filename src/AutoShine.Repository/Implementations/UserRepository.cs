using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoShine.Repository.Implementations;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role)
        => await _dbSet.Where(u => u.Role == role && u.IsActive).ToListAsync();

    public async Task<IEnumerable<User>> GetAvailableEmployeesAsync(DateTime startTime, DateTime endTime)
    {
        var dayOfWeek = startTime.DayOfWeek;
        var timeStart = startTime.TimeOfDay;
        var timeEnd = endTime.TimeOfDay;

        // Employees who have a schedule covering the requested slot
        var scheduledEmployeeIds = await _context.EmployeeSchedules
            .Where(s => s.DayOfWeek == dayOfWeek
                     && s.IsAvailable
                     && s.StartTime <= timeStart
                     && s.EndTime >= timeEnd)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync();

        // Filter out those who already have a booking in that window
        var bookedEmployeeIds = await _context.Bookings
            .Where(b => b.EmployeeId.HasValue
                     && b.Status != BookingStatus.Cancelled
                     && b.StartTime < endTime
                     && b.EndTime > startTime)
            .Select(b => b.EmployeeId!.Value)
            .Distinct()
            .ToListAsync();

        var availableIds = scheduledEmployeeIds.Except(bookedEmployeeIds).ToList();

        return await _dbSet
            .Where(u => u.Role == UserRole.Employee && u.IsActive && availableIds.Contains(u.Id))
            .ToListAsync();
    }
}
