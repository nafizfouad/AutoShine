using MapsterMapper;
using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.DTOs.Reviews;
using AutoShine.Service.Interfaces;

namespace AutoShine.Service.Implementations;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReviewService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ReviewDto>> GetReviewsByEmployeeAsync(int employeeId)
    {
        var reviews = await _unitOfWork.Reviews.GetReviewsByEmployeeAsync(employeeId);
        return _mapper.Map<IEnumerable<ReviewDto>>(reviews);
    }

    public async Task<ReviewDto?> GetReviewByBookingAsync(int bookingId)
    {
        var review = await _unitOfWork.Reviews.GetReviewByBookingAsync(bookingId);
        return review == null ? null : _mapper.Map<ReviewDto>(review);
    }

    public async Task<ReviewDto> CreateReviewAsync(int customerId, CreateReviewDto dto)
    {
        // Only allow reviews on completed bookings
        var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(dto.BookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.CustomerId != customerId)
            throw new UnauthorizedAccessException("You can only review your own bookings.");

        if (booking.Status != BookingStatus.Completed)
            throw new InvalidOperationException("You can only review completed bookings.");

        var existing = await _unitOfWork.Reviews.GetReviewByBookingAsync(dto.BookingId);
        if (existing != null)
            throw new InvalidOperationException("A review for this booking already exists.");

        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var review = new Review
        {
            BookingId = dto.BookingId,
            CustomerId = customerId,
            EmployeeId = dto.EmployeeId,
            Rating = dto.Rating,
            Comment = dto.Comment
        };

        await _unitOfWork.Reviews.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReviewDto>((await _unitOfWork.Reviews.GetReviewByBookingAsync(dto.BookingId))!);
    }

    public async Task<bool> DeleteReviewAsync(int reviewId)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
        if (review == null) return false;

        _unitOfWork.Reviews.Remove(review);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
