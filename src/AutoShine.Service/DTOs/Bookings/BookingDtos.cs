using AutoShine.Models.Enums;

namespace AutoShine.Service.DTOs.Bookings;

public record BookingDto(
    int Id,
    int CustomerId,
    string CustomerName,
    int? EmployeeId,
    string? EmployeeName,
    int PackageId,
    string PackageName,
    decimal PackagePrice,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    string? Notes,
    DateTime CreatedAt
);

public record CreateBookingDto(
    int PackageId,
    DateTime StartTime,
    int? PreferredEmployeeId, // null = Any Available
    string? Notes
);

public record UpdateBookingStatusDto(
    BookingStatus Status
);

public record AvailableSlotDto(
    DateTime StartTime,
    DateTime EndTime,
    List<int> AvailableEmployeeIds
);

public record AvailableSlotsRequestDto(
    int PackageId,
    DateTime Date
);
