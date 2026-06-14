using AutoShine.Service.DTOs.Bookings;
using AutoShine.Service.DTOs.Common;
using AutoShine.Models.Enums;

namespace AutoShine.Service.Interfaces;

public interface IBookingService
{
    Task<PagedResult<BookingDto>> GetAllBookingsAsync(int page, int pageSize, BookingStatus? status = null);
    Task<IEnumerable<BookingDto>> GetCustomerBookingsAsync(int customerId);
    Task<IEnumerable<BookingDto>> GetEmployeeBookingsAsync(int employeeId);
    Task<BookingDto?> GetBookingByIdAsync(int bookingId);
    Task<BookingDto> CreateBookingAsync(int customerId, CreateBookingDto dto);
    Task<BookingDto?> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus, int actorUserId);
    Task<bool> CancelBookingAsync(int bookingId, int actorUserId);
    Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(AvailableSlotsRequestDto request);
}
