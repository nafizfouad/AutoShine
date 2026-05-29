using AutoShine.Models.Entities;
using AutoShine.Models.Enums;

namespace AutoShine.Repository.Interfaces;

public interface IBookingRepository : IGenericRepository<Booking>
{
    Task<IEnumerable<Booking>> GetBookingsByCustomerAsync(int customerId);
    Task<IEnumerable<Booking>> GetBookingsByEmployeeAsync(int employeeId);
    Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime start, DateTime end);
    Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status);
    Task<Booking?> GetBookingWithDetailsAsync(int bookingId);
    Task<bool> IsEmployeeAvailableAsync(int employeeId, DateTime startTime, DateTime endTime, int? excludeBookingId = null);
}
