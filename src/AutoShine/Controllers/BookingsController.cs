using AutoShine.Common;
using AutoShine.Models.Enums;
using AutoShine.Service.DTOs.Bookings;
using AutoShine.Service.DTOs.Common;
using AutoShine.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoShine.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetCurrentUserRole() =>
        User.FindFirstValue(ClaimTypes.Role)!;

    /// <summary>Get all bookings with optional status filter (Admin only).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<BookingDto>>>> GetAllBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] BookingStatus? status = null)
    {
        var result = await _bookingService.GetAllBookingsAsync(page, pageSize, status);
        return Ok(ApiResponse<PagedResult<BookingDto>>.Ok(result));
    }

    /// <summary>Get my bookings (Customer) or assigned bookings (Employee).</summary>
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> GetMyBookings()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        IEnumerable<BookingDto> bookings = role switch
        {
            "Employee" => await _bookingService.GetEmployeeBookingsAsync(userId),
            _ => await _bookingService.GetCustomerBookingsAsync(userId)
        };

        return Ok(ApiResponse<IEnumerable<BookingDto>>.Ok(bookings));
    }

    /// <summary>Get a specific booking by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> GetBooking(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null) return NotFound(ApiResponse<BookingDto>.Fail("Booking not found."));
        return Ok(ApiResponse<BookingDto>.Ok(booking));
    }

    /// <summary>Get available time slots for a package on a given date.</summary>
    [HttpGet("available-slots")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AvailableSlotDto>>>> GetAvailableSlots(
        [FromQuery] int packageId,
        [FromQuery] DateTime date)
    {
        var slots = await _bookingService.GetAvailableSlotsAsync(new AvailableSlotsRequestDto(packageId, date));
        return Ok(ApiResponse<IEnumerable<AvailableSlotDto>>.Ok(slots));
    }

    /// <summary>Create a new booking (Customer).</summary>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> CreateBooking([FromBody] CreateBookingDto dto)
    {
        var customerId = GetCurrentUserId();
        var booking = await _bookingService.CreateBookingAsync(customerId, dto);
        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id },
            ApiResponse<BookingDto>.Ok(booking, "Booking created."));
    }

    /// <summary>Update a booking's status (Employee or Admin).</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> UpdateStatus(int id, [FromBody] UpdateBookingStatusDto dto)
    {
        var actorId = GetCurrentUserId();
        var booking = await _bookingService.UpdateBookingStatusAsync(id, dto.Status, actorId);
        if (booking == null) return NotFound(ApiResponse<BookingDto>.Fail("Booking not found."));
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Status updated."));
    }

    /// <summary>Cancel a booking.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelBooking(int id)
    {
        var actorId = GetCurrentUserId();
        var success = await _bookingService.CancelBookingAsync(id, actorId);
        if (!success) return NotFound(ApiResponse<bool>.Fail("Booking not found."));
        return Ok(ApiResponse<bool>.Ok(true, "Booking cancelled."));
    }
}
