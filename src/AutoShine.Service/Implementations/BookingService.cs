using MapsterMapper;
using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.DTOs.Bookings;
using AutoShine.Service.DTOs.Common;
using AutoShine.Service.Interfaces;

namespace AutoShine.Service.Implementations;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    // Shop hours: 08:00 – 18:00
    private static readonly TimeSpan ShopOpen = new(8, 0, 0);
    private static readonly TimeSpan ShopClose = new(18, 0, 0);

    public BookingService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<BookingDto>> GetAllBookingsAsync(int page, int pageSize, BookingStatus? status = null)
    {
        var (items, total) = await _unitOfWork.Bookings.GetPagedAsync(
            page, pageSize,
            filter: b => status == null || b.Status == status,
            orderBy: q => q.OrderByDescending(b => b.StartTime),
            includeProperties: "Customer,Employee,Package");

        return new PagedResult<BookingDto>
        {
            Items = _mapper.Map<IEnumerable<BookingDto>>(items),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<BookingDto>> GetCustomerBookingsAsync(int customerId)
    {
        var bookings = await _unitOfWork.Bookings.GetBookingsByCustomerAsync(customerId);
        return _mapper.Map<IEnumerable<BookingDto>>(bookings);
    }

    public async Task<IEnumerable<BookingDto>> GetEmployeeBookingsAsync(int employeeId)
    {
        var bookings = await _unitOfWork.Bookings.GetBookingsByEmployeeAsync(employeeId);
        return _mapper.Map<IEnumerable<BookingDto>>(bookings);
    }

    public async Task<BookingDto?> GetBookingByIdAsync(int bookingId)
    {
        var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
        return booking == null ? null : _mapper.Map<BookingDto>(booking);
    }

    public async Task<BookingDto> CreateBookingAsync(int customerId, CreateBookingDto dto)
    {
        var package = await _unitOfWork.Packages.GetPackageWithItemsAsync(dto.PackageId)
            ?? throw new KeyNotFoundException("Package not found.");

        var endTime = dto.StartTime.AddMinutes(package.EstimatedDurationMinutes);

        int? assignedEmployeeId;

        if (dto.PreferredEmployeeId.HasValue)
        {
            // Validate preferred employee is available
            var available = await _unitOfWork.Bookings.IsEmployeeAvailableAsync(
                dto.PreferredEmployeeId.Value, dto.StartTime, endTime);
            if (!available)
                throw new InvalidOperationException("Preferred employee is not available at that time.");
            assignedEmployeeId = dto.PreferredEmployeeId.Value;
        }
        else
        {
            // Round-robin / least-busy assignment
            var availableEmployees = await _unitOfWork.Users.GetAvailableEmployeesAsync(dto.StartTime, endTime);
            var employees = availableEmployees.ToList();
            if (!employees.Any())
                throw new InvalidOperationException("No employees available for the selected time slot.");

            // Pick least-busy employee (fewest confirmed/in-progress bookings this day)
            var dayStart = dto.StartTime.Date;
            var dayEnd = dayStart.AddDays(1);
            var dayBookings = await _unitOfWork.Bookings.GetBookingsByDateRangeAsync(dayStart, dayEnd);

            assignedEmployeeId = employees
                .OrderBy(e => dayBookings.Count(b =>
                    b.EmployeeId == e.Id &&
                    b.Status != BookingStatus.Cancelled))
                .First().Id;
        }

        var booking = new Booking
        {
            CustomerId = customerId,
            EmployeeId = assignedEmployeeId,
            PackageId = dto.PackageId,
            StartTime = dto.StartTime,
            EndTime = endTime,
            Status = BookingStatus.Pending,
            Notes = dto.Notes
        };

        await _unitOfWork.Bookings.AddAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<BookingDto>(await _unitOfWork.Bookings.GetBookingWithDetailsAsync(booking.Id)
            ?? throw new InvalidOperationException("Failed to load created booking."));
    }

    public async Task<BookingDto?> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus, int actorUserId)
    {
        var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
        if (booking == null) return null;

        // If marking Completed, deduct inventory atomically
        if (newStatus == BookingStatus.Completed && booking.Status == BookingStatus.InProgress)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                booking.Status = newStatus;
                _unitOfWork.Bookings.Update(booking);

                foreach (var pi in booking.Package.PackageItems)
                {
                    var success = await _unitOfWork.Inventory.DeductStockAsync(pi.InventoryItemId, pi.QuantityRequired);
                    if (!success)
                        throw new InvalidOperationException(
                            $"Insufficient stock for item: {pi.InventoryItem.ItemName}");
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        else
        {
            booking.Status = newStatus;
            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();
        }

        return _mapper.Map<BookingDto>((await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId))!);
    }

    public async Task<bool> CancelBookingAsync(int bookingId, int actorUserId)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null) return false;

        if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel a completed or already cancelled booking.");

        booking.Status = BookingStatus.Cancelled;
        _unitOfWork.Bookings.Update(booking);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(AvailableSlotsRequestDto request)
    {
        var package = await _unitOfWork.Packages.GetByIdAsync(request.PackageId)
            ?? throw new KeyNotFoundException("Package not found.");

        var duration = TimeSpan.FromMinutes(package.EstimatedDurationMinutes);
        var date = request.Date.Date;

        // Get templates active on this date
        var templates = (await _unitOfWork.Schedules.GetActiveTemplatesForDateAsync(date))
            .Where(t => t.CoversDay(date.DayOfWeek))
            .ToList();

        if (!templates.Any()) return Enumerable.Empty<AvailableSlotDto>();

        // Existing bookings on this date (non-cancelled)
        var dayBookings = (await _unitOfWork.Bookings.GetBookingsByDateRangeAsync(date, date.AddDays(1)))
            .Where(b => b.Status != AutoShine.Models.Enums.BookingStatus.Cancelled)
            .ToList();

        // Build a set of 30-min candidate slots across union of all employee windows
        var earliestStart = templates.Min(t => date + t.WorkStartTime);
        var latestEnd     = templates.Max(t => date + t.WorkEndTime);

        var slotMap = new SortedDictionary<DateTime, List<AvailableEmployeeInfo>>();

        var cursor = earliestStart;
        while (cursor + duration <= latestEnd)
        {
            var slotEnd = cursor + duration;
            var availableForSlot = new List<AvailableEmployeeInfo>();

            foreach (var tmpl in templates)
            {
                var wStart = date + tmpl.WorkStartTime;
                var wEnd   = date + tmpl.WorkEndTime;

                // Employee window must cover entire slot
                if (cursor < wStart || slotEnd > wEnd) goto next;

                // Exclude break window (any overlap)
                if (tmpl.BreakStartTime.HasValue && tmpl.BreakEndTime.HasValue)
                {
                    var bStart = date + tmpl.BreakStartTime.Value;
                    var bEnd   = date + tmpl.BreakEndTime.Value;
                    if (cursor < bEnd && slotEnd > bStart) goto next;
                }

                // Check leave
                if (await _unitOfWork.Schedules.IsOnLeaveAsync(tmpl.EmployeeId, date)) goto next;

                // Check booking overlap
                var hasConflict = dayBookings.Any(b =>
                    b.EmployeeId == tmpl.EmployeeId &&
                    b.StartTime < slotEnd && b.EndTime > cursor);

                if (!hasConflict)
                    availableForSlot.Add(new AvailableEmployeeInfo(tmpl.EmployeeId,
                        $"{tmpl.Employee.FirstName} {tmpl.Employee.LastName}"));

                next:;
            }

            if (availableForSlot.Any())
                slotMap[cursor] = availableForSlot;

            cursor = cursor.AddMinutes(30);
        }

        return slotMap.Select(kv => new AvailableSlotDto(kv.Key, kv.Key + duration, kv.Value));
    }
}
