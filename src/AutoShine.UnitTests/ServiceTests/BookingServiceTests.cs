using AutoShine.Models.Enums;
using AutoShine.Service.DTOs.Bookings;
using AutoShine.Service.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.ServiceTests;

public class BookingServiceTests
{
    // ── Helper: seed an employee with a schedule template covering today ──────
    private static async Task<ServiceFixture> BuildFixtureWithScheduleAsync(
        bool addEmployee = true, bool addTemplate = true)
    {
        var fx = new ServiceFixture();
        var emp = Seed.Employee(1);
        var cust = Seed.Customer(100);
        var pkg = Seed.Package(1, durationMin: 60);

        // Make StartTime/EndTime UTC to avoid PostgreSQL kind issues with InMemory
        var template = Seed.Template(1, emp.Id, workingDays: 0b1111111 /* every day */);

        if (addEmployee) fx.Ctx.Users.AddRange(emp, cust);
        fx.Ctx.Packages.Add(pkg);
        if (addTemplate) fx.Ctx.EmployeeScheduleTemplates.Add(template);
        await fx.Ctx.SaveChangesAsync();
        return fx;
    }

    // ── GetCustomerBookingsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetCustomerBookingsAsync_ReturnsOnlyOwnBookings()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        var cust2 = Seed.Customer(200, "Carol", "White");
        fx.Ctx.Users.Add(cust2);
        var b1 = Seed.Booking(1, 100, 1, 1);
        var b2 = Seed.Booking(2, 200, 1, 1);
        fx.Ctx.Bookings.AddRange(b1, b2);
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = (await svc.GetCustomerBookingsAsync(100)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetCustomerBookingsAsync_DtoHasHasReviewFalse_WhenNoReview()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, BookingStatus.Completed));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = (await svc.GetCustomerBookingsAsync(100)).ToList();

        Assert.False(result[0].HasReview);
    }

    [Fact]
    public async Task GetCustomerBookingsAsync_DtoHasHasReviewTrue_WhenReviewExists()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        var booking = Seed.Booking(1, 100, 1, 1, BookingStatus.Completed);
        fx.Ctx.Bookings.Add(booking);
        await fx.Ctx.SaveChangesAsync();
        fx.Ctx.Reviews.Add(Seed.Review(1, 1, 100, 1));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = (await svc.GetCustomerBookingsAsync(100)).ToList();

        Assert.True(result[0].HasReview);
    }

    // ── GetEmployeeBookingsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetEmployeeBookingsAsync_ReturnsOnlyAssignedBookings()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        var emp2 = Seed.Employee(2, "Dave", "Brown");
        fx.Ctx.Users.Add(emp2);
        fx.Ctx.Bookings.AddRange(
            Seed.Booking(1, 100, 1, 1),
            Seed.Booking(2, 100, 2, 1));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = (await svc.GetEmployeeBookingsAsync(1)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    // ── GetBookingByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetBookingByIdAsync_Found_ReturnsDto()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = await svc.GetBookingByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NotFound_ReturnsNull()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);
        Assert.Null(await svc.GetBookingByIdAsync(999));
    }

    // ── UpdateBookingStatusAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateBookingStatusAsync_PendingToConfirmed_Succeeds()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, BookingStatus.Pending));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = await svc.UpdateBookingStatusAsync(1, BookingStatus.Confirmed, actorUserId: 1);

        Assert.NotNull(result);
        Assert.Equal("Confirmed", result!.Status);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_InProgressToCompleted_DeductsInventory()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        var invItem = Seed.InventoryItem(1, stock: 10);
        fx.Ctx.InventoryItems.Add(invItem);
        var pkg = await fx.Ctx.Packages.FindAsync(1);
        fx.Ctx.PackageItems.Add(new AutoShine.Models.Entities.PackageItem
        {
            Id = 1, PackageId = 1, InventoryItemId = 1, QuantityRequired = 3
        });
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, BookingStatus.InProgress));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = await svc.UpdateBookingStatusAsync(1, BookingStatus.Completed, actorUserId: 1);

        Assert.Equal("Completed", result!.Status);
        var updatedItem = await fx.Uow.Inventory.GetByIdAsync(1);
        Assert.Equal(7, updatedItem!.CurrentStock); // 10 - 3
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_InsufficientInventory_ThrowsAndRollsBack()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        var invItem = Seed.InventoryItem(1, stock: 1); // only 1 unit
        fx.Ctx.InventoryItems.Add(invItem);
        fx.Ctx.PackageItems.Add(new AutoShine.Models.Entities.PackageItem
        {
            Id = 1, PackageId = 1, InventoryItemId = 1, QuantityRequired = 5 // needs 5
        });
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, BookingStatus.InProgress));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.UpdateBookingStatusAsync(1, BookingStatus.Completed, actorUserId: 1));
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_NotFound_ReturnsNull()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);
        Assert.Null(await svc.UpdateBookingStatusAsync(999, BookingStatus.Confirmed, 1));
    }

    // ── CancelBookingAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CancelBookingAsync_PendingBooking_Cancels()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, BookingStatus.Pending));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = await svc.CancelBookingAsync(1, actorUserId: 100);

        Assert.True(result);
        var booking = await fx.Uow.Bookings.GetByIdAsync(1);
        Assert.Equal(BookingStatus.Cancelled, booking!.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_CompletedBooking_Throws()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, BookingStatus.Completed));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelBookingAsync(1, actorUserId: 100));
    }

    [Fact]
    public async Task CancelBookingAsync_AlreadyCancelled_Throws()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        fx.Ctx.Bookings.Add(Seed.Booking(1, 100, 1, 1, BookingStatus.Cancelled));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelBookingAsync(1, actorUserId: 100));
    }

    [Fact]
    public async Task CancelBookingAsync_NotFound_ReturnsFalse()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        var svc = new BookingService(fx.Uow, fx.Mapper);
        Assert.False(await svc.CancelBookingAsync(999, actorUserId: 100));
    }

    // ── GetAllBookingsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllBookingsAsync_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        fx.Ctx.Bookings.AddRange(
            Seed.Booking(1, 100, 1, 1, BookingStatus.Pending),
            Seed.Booking(2, 100, 1, 1, BookingStatus.Confirmed));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = await svc.GetAllBookingsAsync(1, 10, BookingStatus.Pending);

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, b => Assert.Equal("Pending", b.Status));
    }

    [Fact]
    public async Task GetAllBookingsAsync_NoFilter_ReturnsAll()
    {
        using var fx = await BuildFixtureWithScheduleAsync();
        fx.Ctx.Bookings.AddRange(
            Seed.Booking(1, 100, 1, 1, BookingStatus.Pending),
            Seed.Booking(2, 100, 1, 1, BookingStatus.Completed));
        await fx.Ctx.SaveChangesAsync();

        var svc = new BookingService(fx.Uow, fx.Mapper);
        var result = await svc.GetAllBookingsAsync(1, 10);

        Assert.Equal(2, result.TotalCount);
    }
}
