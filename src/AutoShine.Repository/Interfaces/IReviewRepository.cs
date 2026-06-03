using AutoShine.Models.Entities;

namespace AutoShine.Repository.Interfaces;

public interface IReviewRepository : IGenericRepository<Review>
{
    Task<IEnumerable<Review>> GetReviewsByEmployeeAsync(int employeeId);
    Task<Review?> GetReviewByBookingAsync(int bookingId);
    Task<double> GetAverageRatingForEmployeeAsync(int employeeId);
}
