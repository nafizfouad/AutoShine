using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoShine.Repository.Implementations;

public class BookingRepository : GenericRepository<Booking>, IBookingRepository
{
    public BookingRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Booking>> GetBookingsByCustomerAsync(int customerId)
        => await _dbSet
            .Include(b => b.Package)
            .Include(b => b.Employee)
            .Include(b => b.Review)
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.StartTime)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetBookingsByEmployeeAsync(int employeeId)
        => await _dbSet
            .Include(b => b.Package)
            .Include(b => b.Customer)
            .Where(b => b.EmployeeId == employeeId)
            .OrderByDescending(b => b.StartTime)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime start, DateTime end)
        => await _dbSet
            .Include(b => b.Customer)
            .Include(b => b.Employee)
            .Include(b => b.Package)
            .Where(b => b.StartTime >= start && b.StartTime <= end)
            .OrderBy(b => b.StartTime)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status)
        => await _dbSet
            .Include(b => b.Customer)
            .Include(b => b.Employee)
            .Include(b => b.Package)
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.StartTime)
            .ToListAsync();

    public async Task<Booking?> GetBookingWithDetailsAsync(int bookingId)
        => await _dbSet
            .Include(b => b.Customer)
            .Include(b => b.Employee)
            .Include(b => b.Package)
                .ThenInclude(p => p.PackageItems)
                    .ThenInclude(pi => pi.InventoryItem)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

    public async Task<bool> IsEmployeeAvailableAsync(int employeeId, DateTime startTime, DateTime endTime, int? excludeBookingId = null)
    {
        var query = _dbSet.Where(b =>
            b.EmployeeId == employeeId &&
            b.Status != BookingStatus.Cancelled &&
            b.StartTime < endTime &&
            b.EndTime > startTime);

        if (excludeBookingId.HasValue)
            query = query.Where(b => b.Id != excludeBookingId.Value);

        return !await query.AnyAsync();
    }
}
