using MapsterMapper;
using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.Common;
using AutoShine.Service.DTOs.Bookings;
using AutoShine.Service.DTOs.Common;
using AutoShine.Service.Interfaces;

namespace AutoShine.Service.Implementations;

public class BookingService : IBookingService
{
    private static readonly TimeSpan SlotInterval = TimeSpan.FromMinutes(30);

    // The only forward transitions allowed through the status endpoint.
    private static readonly Dictionary<BookingStatus, BookingStatus> NextStatus = new()
    {
        [BookingStatus.Pending] = BookingStatus.Confirmed,
        [BookingStatus.Confirmed] = BookingStatus.InProgress,
        [BookingStatus.InProgress] = BookingStatus.Completed,
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ShopSettings _shop;

    public BookingService(IUnitOfWork unitOfWork, IMapper mapper, ShopSettings? shop = null)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _shop = shop ?? new ShopSettings();
    }

    public async Task<PagedResult<BookingDto>> GetAllBookingsAsync(int page, int pageSize, BookingStatus? status = null)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var (items, total) = await _unitOfWork.Bookings.GetPagedAsync(
            page, pageSize,
            filter: b => status == null || b.Status == status,
            orderBy: q => q.OrderByDescending(b => b.StartTime),
            includeProperties: "Customer,Employee,Package,Review");

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
        var package = await _unitOfWork.Packages.GetByIdAsync(dto.PackageId)
            ?? throw new KeyNotFoundException("Package not found.");
        if (!package.IsActive)
            throw new InvalidOperationException("This package is no longer available.");

        var notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        if (notes?.Length > 500)
            throw new ArgumentException("Notes must be at most 500 characters.");

        var startTime = DateTimeUtil.AsUtcInstant(dto.StartTime);
        if (startTime <= DateTime.UtcNow)
            throw new InvalidOperationException("Bookings must be for a future time.");
        var endTime = startTime.AddMinutes(package.EstimatedDurationMinutes);

        // Same availability rules as the slot finder: schedule template, break, leave, existing bookings.
        var day = await LoadDayAsync(_shop.ToShopDate(startTime));
        var free = FindFreeEmployees(day, startTime, endTime);
        if (free.Count == 0)
            throw new InvalidOperationException("No employees are available for the selected time slot.");

        int assignedEmployeeId;
        if (dto.PreferredEmployeeId is int preferredId)
        {
            if (free.All(e => e.Id != preferredId))
                throw new InvalidOperationException("The selected employee is not available at that time.");
            assignedEmployeeId = preferredId;
        }
        else
        {
            // Least-busy assignment: fewest non-cancelled bookings on that shop day.
            assignedEmployeeId = free
                .OrderBy(e => day.Bookings.Count(b =>
                    b.EmployeeId == e.Id && _shop.ToShopDate(b.StartTime) == day.Date))
                .ThenBy(e => e.Id)
                .First().Id;
        }

        var booking = new Booking
        {
            CustomerId = customerId,
            EmployeeId = assignedEmployeeId,
            PackageId = dto.PackageId,
            StartTime = startTime,
            EndTime = endTime,
            Status = BookingStatus.Pending,
            Notes = notes
        };

        await _unitOfWork.Bookings.AddAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<BookingDto>(await _unitOfWork.Bookings.GetBookingWithDetailsAsync(booking.Id)
            ?? throw new InvalidOperationException("Failed to load created booking."));
    }

    public async Task<BookingDto?> UpdateBookingStatusAsync(
        int bookingId, BookingStatus newStatus, int actorUserId, UserRole actorRole)
    {
        var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
        if (booking == null) return null;

        var canManage = actorRole == UserRole.Admin
            || (actorRole == UserRole.Employee && booking.EmployeeId == actorUserId);
        if (!canManage)
            throw new UnauthorizedAccessException("You can only update bookings assigned to you.");

        if (newStatus == BookingStatus.Cancelled)
        {
            EnsureCancellable(booking);
            booking.Status = BookingStatus.Cancelled;
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            if (!NextStatus.TryGetValue(booking.Status, out var expected) || expected != newStatus)
                throw new InvalidOperationException($"Cannot change a {booking.Status} booking to {newStatus}.");

            if (newStatus == BookingStatus.Completed)
            {
                // Marking Completed deducts the package's inventory atomically.
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    booking.Status = newStatus;

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
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return _mapper.Map<BookingDto>((await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId))!);
    }

    public async Task<bool> CancelBookingAsync(int bookingId, int actorUserId, UserRole actorRole)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null) return false;

        var allowed = actorRole switch
        {
            UserRole.Admin => true,
            UserRole.Employee => booking.EmployeeId == actorUserId,
            UserRole.Customer => booking.CustomerId == actorUserId,
            _ => false
        };
        if (!allowed)
            throw new UnauthorizedAccessException("You can only cancel your own bookings.");

        EnsureCancellable(booking);
        booking.Status = BookingStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(AvailableSlotsRequestDto request)
    {
        var package = await _unitOfWork.Packages.GetByIdAsync(request.PackageId)
            ?? throw new KeyNotFoundException("Package not found.");
        if (!package.IsActive)
            throw new InvalidOperationException("This package is no longer available.");

        var duration = TimeSpan.FromMinutes(package.EstimatedDurationMinutes);
        var day = await LoadDayAsync(request.Date);
        if (day.Templates.Count == 0) return Enumerable.Empty<AvailableSlotDto>();

        // 30-minute candidates across the union of all employee windows (shop-local wall clock).
        var earliestStart = day.Templates.Min(t => t.WorkStartTime);
        var latestEnd = day.Templates.Max(t => t.WorkEndTime);
        var now = DateTime.UtcNow;

        var slots = new List<AvailableSlotDto>();
        for (var local = earliestStart; local + duration <= latestEnd; local += SlotInterval)
        {
            var start = _shop.ToUtc(request.Date, local);
            if (start == null || start <= now) continue;

            var end = start.Value + duration;
            var free = FindFreeEmployees(day, start.Value, end);
            if (free.Count > 0)
                slots.Add(new AvailableSlotDto(start.Value, end, free));
        }

        return slots;
    }

    // ── Availability helpers ──────────────────────────────────────────────────

    private sealed record DayContext(
        DateOnly Date,
        List<EmployeeScheduleTemplate> Templates,
        HashSet<int> OnLeave,
        List<Booking> Bookings);

    private async Task<DayContext> LoadDayAsync(DateOnly date)
    {
        var dateKey = DateTimeUtil.AsUtcDate(date);

        var templates = (await _unitOfWork.Schedules.GetActiveTemplatesForDateAsync(dateKey))
            .Where(t => t.CoversDay(date.DayOfWeek)
                     && t.Employee.IsActive
                     && t.Employee.Role == UserRole.Employee)
            .ToList();

        var onLeave = await _unitOfWork.Schedules.GetEmployeeIdsOnLeaveAsync(dateKey);

        // A ±1 day window around the date covers every UTC offset; only overlaps matter below.
        var bookings = (await _unitOfWork.Bookings.GetBookingsByDateRangeAsync(dateKey.AddDays(-1), dateKey.AddDays(2)))
            .Where(b => b.Status != BookingStatus.Cancelled)
            .ToList();

        return new DayContext(date, templates, onLeave, bookings);
    }

    private List<AvailableEmployeeInfo> FindFreeEmployees(DayContext day, DateTime startUtc, DateTime endUtc)
    {
        var free = new List<AvailableEmployeeInfo>();

        foreach (var templates in day.Templates.GroupBy(t => t.EmployeeId))
        {
            var employeeId = templates.Key;
            if (day.OnLeave.Contains(employeeId)) continue;
            if (!templates.Any(t => TemplateCovers(t, day.Date, startUtc, endUtc))) continue;

            var hasConflict = day.Bookings.Any(b =>
                b.EmployeeId == employeeId && b.StartTime < endUtc && b.EndTime > startUtc);
            if (hasConflict) continue;

            var employee = templates.First().Employee;
            free.Add(new AvailableEmployeeInfo(employeeId, $"{employee.FirstName} {employee.LastName}"));
        }

        return free;
    }

    private bool TemplateCovers(EmployeeScheduleTemplate t, DateOnly date, DateTime startUtc, DateTime endUtc)
    {
        // Work window must cover the entire slot.
        var workStart = _shop.ToUtc(date, t.WorkStartTime);
        var workEnd = _shop.ToUtc(date, t.WorkEndTime);
        if (workStart == null || workEnd == null || startUtc < workStart || endUtc > workEnd)
            return false;

        // Slot must not overlap the break.
        if (t.BreakStartTime is TimeSpan breakStartLocal && t.BreakEndTime is TimeSpan breakEndLocal)
        {
            var breakStart = _shop.ToUtc(date, breakStartLocal);
            var breakEnd = _shop.ToUtc(date, breakEndLocal);
            if (breakStart != null && breakEnd != null && startUtc < breakEnd && endUtc > breakStart)
                return false;
        }

        return true;
    }

    private static void EnsureCancellable(Booking booking)
    {
        if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel a completed or already cancelled booking.");
    }
}
