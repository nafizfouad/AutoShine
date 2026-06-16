using AutoShine.Service.DTOs.Reviews;

namespace AutoShine.Service.Interfaces;

public interface IReviewService
{
    Task<IEnumerable<ReviewDto>> GetReviewsByEmployeeAsync(int employeeId);
    Task<ReviewDto?> GetReviewByBookingAsync(int bookingId);
    Task<ReviewDto> CreateReviewAsync(int customerId, CreateReviewDto dto);
    Task<bool> DeleteReviewAsync(int reviewId);
}
