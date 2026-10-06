using AutoShine.Models.Enums;
using AutoShine.Service.Common;
using AutoShine.Service.DTOs.Bookings;
using AutoShine.Service.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.ServiceTests;

/// <summary>Booking creation, slot finding, status transitions and access rules.</summary>
public class BookingAvailabilityTests
{
    private static readonly DateOnly Tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

    private static DateTime TomorrowAt(int hour) =>
        DateTime.SpecifyKind(Tomorrow.ToDateTime(new TimeOnly(hour, 0)), DateTimeKind.Utc);

    // Two employees working every day 09:00–17:00 (break 13:00–14:00 for employee 1), one customer.
    private static async Task<ServiceFixture> BuildAsync()
    {
        var fx = new ServiceFixture();
        var t1 = Seed.Template(1, 1, workingDays: 0b1111111);
        t1.BreakStartTime = new TimeSpan(13, 0, 0);
        t1.BreakEndTime = new TimeSpan(14, 0, 0);
        fx.Ctx.Users.AddRange(Seed.Employee(1), Seed.Employee(2, "Dave", "Brown"), Seed.Customer(100), Seed.Customer(101));
        fx.Ctx.Packages.AddRange(Seed.Package(1, durationMin: 60), Seed.Package(2, active: false));
        fx.Ctx.EmployeeScheduleTemplates.AddRange(t1, Seed.Template(2, 2, workingDays: 0b1111111));
        await fx.Ctx.SaveChangesAsync();
        return fx;
    }

    // ── CreateBookingAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateBooking_NoPreference_AutoAssignsFromScheduleTemplates()
    {
        using var fx = await BuildAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        var result = await svc.CreateBookingAsync(100, new CreateBookingDto(1, TomorrowAt(10), null, null));

        Assert.NotNull(result.EmployeeId);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreateBooking_NoPreference_PicksLeastBusyEmployee()
    {
        using var fx = await BuildAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, start: TomorrowAt(9)));
        await fx.Ctx.SaveChangesAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        var result = await svc.CreateBookingAsync(101, new CreateBookingDto(1, TomorrowAt(15), null, null));

        Assert.Equal(2, result.EmployeeId);
    }

    [Fact]
    public async Task CreateBooking_PreferredEmployeeOnBreak_Throws()
    {
        using var fx = await BuildAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateBookingAsync(100, new CreateBookingDto(1, TomorrowAt(13), PreferredEmployeeId: 1, null)));
    }

    [Fact]
    public async Task CreateBooking_EmployeeOnLeave_IsNotAssigned()
    {
        using var fx = await BuildAsync();
        fx.Ctx.EmployeeLeaves.Add(Seed.Leave(1, 2, DateTimeUtil.AsUtcDate(Tomorrow)));
        await fx.Ctx.SaveChangesAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        var result = await svc.CreateBookingAsync(100, new CreateBookingDto(1, TomorrowAt(10), null, null));

        Assert.Equal(1, result.EmployeeId);
    }

    [Fact]
    public async Task CreateBooking_OutsideWorkingHours_Throws()
    {
        using var fx = await BuildAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateBookingAsync(100, new CreateBookingDto(1, TomorrowAt(17), null, null)));
    }

    [Fact]
    public async Task CreateBooking_InThePast_Throws()
    {
        using var fx = await BuildAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateBookingAsync(100, new CreateBookingDto(1, DateTime.UtcNow.AddDays(-1), null, null)));
    }

    [Fact]
    public async Task CreateBooking_InactivePackage_Throws()
    {
        using var fx = await BuildAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateBookingAsync(100, new CreateBookingDto(2, TomorrowAt(10), null, null)));
    }

    [Fact]
    public async Task CreateBooking_UnspecifiedKind_IsStoredAsUtc()
    {
        using var fx = await BuildAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);
        var unspecified = DateTime.SpecifyKind(TomorrowAt(10), DateTimeKind.Unspecified);

        var result = await svc.CreateBookingAsync(100, new CreateBookingDto(1, unspecified, null, null));

        var saved = await fx.Uow.Bookings.GetByIdAsync(result.Id);
        Assert.Equal(DateTimeKind.Utc, saved!.StartTime.Kind);
    }

    // ── GetAvailableSlotsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task Slots_SkipBreakForThatEmployeeOnly()
    {
        using var fx = await BuildAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        var slots = (await svc.GetAvailableSlotsAsync(new AvailableSlotsRequestDto(1, Tomorrow))).ToList();

        var oneOClock = slots.Single(s => s.StartTime == TomorrowAt(13));
        Assert.Equal(new[] { 2 }, oneOClock.AvailableEmployees.Select(e => e.Id));
        Assert.Equal(DateTimeKind.Utc, oneOClock.StartTime.Kind);
    }

    [Fact]
    public async Task Slots_UseShopTimeZone()
    {
        using var fx = await BuildAsync();
        var plusSix = TimeZoneInfo.CreateCustomTimeZone("Test+6", TimeSpan.FromHours(6), "Test+6", "Test+6");
        var svc = new BookingService(fx.Uow, fx.Mapper, new ShopSettings(plusSix));

        var slots = (await svc.GetAvailableSlotsAsync(new AvailableSlotsRequestDto(1, Tomorrow))).ToList();

        // 09:00 shop time (UTC+6) is 03:00 UTC on the same date.
        Assert.Equal(TomorrowAt(3), slots.First().StartTime);
    }

    [Fact]
    public async Task Slots_DeactivatedEmployee_IsExcluded()
    {
        using var fx = await BuildAsync();
        (await fx.Uow.Users.GetByIdAsync(2))!.IsActive = false;
        await fx.Ctx.SaveChangesAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        var slots = await svc.GetAvailableSlotsAsync(new AvailableSlotsRequestDto(1, Tomorrow));

        Assert.All(slots, s => Assert.DoesNotContain(s.AvailableEmployees, e => e.Id == 2));
    }

    // ── Status transitions ───────────────────────────────────────────────────

    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatus.Completed)]
    [InlineData(BookingStatus.Pending, BookingStatus.InProgress)]
    [InlineData(BookingStatus.Completed, BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled, BookingStatus.Confirmed)]
    public async Task UpdateStatus_InvalidTransition_Throws(BookingStatus from, BookingStatus to)
    {
        using var fx = await BuildAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, from));
        await fx.Ctx.SaveChangesAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateBookingStatusAsync(1, to, 1, UserRole.Employee));
    }

    [Fact]
    public async Task UpdateStatus_EmployeeNotAssigned_Throws()
    {
        using var fx = await BuildAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1));
        await fx.Ctx.SaveChangesAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.UpdateBookingStatusAsync(1, BookingStatus.Confirmed, actorUserId: 2, UserRole.Employee));
    }

    [Fact]
    public async Task UpdateStatus_AdminCanManageAnyBooking()
    {
        using var fx = await BuildAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1));
        await fx.Ctx.SaveChangesAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        var result = await svc.UpdateBookingStatusAsync(1, BookingStatus.Confirmed, actorUserId: 999, UserRole.Admin);

        Assert.Equal("Confirmed", result!.Status);
    }

    [Fact]
    public async Task UpdateStatus_ToCancelled_CompletedBooking_Throws()
    {
        using var fx = await BuildAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, BookingStatus.Completed));
        await fx.Ctx.SaveChangesAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateBookingStatusAsync(1, BookingStatus.Cancelled, 1, UserRole.Employee));
    }

    // ── Cancel access ────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_OtherCustomersBooking_Throws()
    {
        using var fx = await BuildAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1));
        await fx.Ctx.SaveChangesAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.CancelBookingAsync(1, actorUserId: 101, UserRole.Customer));
    }

    [Fact]
    public async Task Cancel_UnassignedEmployee_Throws()
    {
        using var fx = await BuildAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1));
        await fx.Ctx.SaveChangesAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.CancelBookingAsync(1, actorUserId: 2, UserRole.Employee));
    }
}
