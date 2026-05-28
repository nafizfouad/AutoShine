using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoShine.Repository.Implementations;

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Review>> GetReviewsByEmployeeAsync(int employeeId)
        => await _dbSet
            .Include(r => r.Customer)
            .Include(r => r.Booking)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<Review?> GetReviewByBookingAsync(int bookingId)
        => await _dbSet
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.BookingId == bookingId);

    public async Task<double> GetAverageRatingForEmployeeAsync(int employeeId)
    {
        var reviews = await _dbSet.Where(r => r.EmployeeId == employeeId).ToListAsync();
        return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
    }
}
