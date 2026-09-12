using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.RepositoryTests;

public class BookingRepositoryTests
{
    private static async Task<(AppDbContext ctx, User emp, User cust, Package pkg)>
        SetupBaseAsync()
    {
        var ctx = DbContextFactory.Create();
        var emp = Seed.Employee(1);
        var cust = Seed.Customer(100);
        var pkg = Seed.Package(1);
        ctx.Users.AddRange(emp, cust);
        ctx.Packages.Add(pkg);
        await ctx.SaveChangesAsync();
        return (ctx, emp, cust, pkg);
    }

    // ── GetBookingsByCustomerAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetBookingsByCustomerAsync_ReturnsOnlyCustomerBookings()
    {
        var (ctx, emp, cust, pkg) = await SetupBaseAsync();
        var cust2 = Seed.Customer(101, "Carol", "White");
        ctx.Users.Add(cust2);
        ctx.Bookings.AddRange(
            Seed.Booking(1, cust.Id, emp.Id, pkg.Id),
            Seed.Booking(2, cust2.Id, emp.Id, pkg.Id));
        await ctx.SaveChangesAsync();

        var repo = new BookingRepository(ctx);
        var result = (await repo.GetBookingsByCustomerAsync(cust.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetBookingsByCustomerAsync_LoadsRequiredNavigations()
    {
        var (ctx, emp, cust, pkg) = await SetupBaseAsync();
        ctx.Bookings.Add(Seed.Booking(1, cust.Id, emp.Id, pkg.Id));
        await ctx.SaveChangesAsync();

        var repo = new BookingRepository(ctx);
        var bookings = (await repo.GetBookingsByCustomerAsync(cust.Id)).ToList();

        Assert.NotNull(bookings[0].Package);
        Assert.NotNull(bookings[0].Customer);
        Assert.NotNull(bookings[0].Employee);
    }

    [Fact]
    public async Task GetBookingsByCustomerAsync_EmptyResult_ReturnsEmpty()
    {
        var (ctx, _, _, _) = await SetupBaseAsync();
        var repo = new BookingRepository(ctx);
        var result = await repo.GetBookingsByCustomerAsync(999);
        Assert.Empty(result);
    }

    // ── GetBookingsByEmployeeAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetBookingsByEmployeeAsync_ReturnsOnlyEmployeeBookings()
    {
        var (ctx, emp, cust, pkg) = await SetupBaseAsync();
        var emp2 = Seed.Employee(2, "Dave", "Brown");
        ctx.Users.Add(emp2);
        ctx.Bookings.AddRange(
            Seed.Booking(1, cust.Id, emp.Id, pkg.Id),
            Seed.Booking(2, cust.Id, emp2.Id, pkg.Id));
        await ctx.SaveChangesAsync();

        var repo = new BookingRepository(ctx);
        var result = (await repo.GetBookingsByEmployeeAsync(emp.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    // ── GetBookingsByDateRangeAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetBookingsByDateRangeAsync_ReturnsBookingsInRange()
    {
        var (ctx, emp, cust, pkg) = await SetupBaseAsync();
        var today = DateTime.UtcNow.Date;
        ctx.Bookings.AddRange(
            Seed.Booking(1, cust.Id, emp.Id, pkg.Id, start: today.AddHours(9)),
            Seed.Booking(2, cust.Id, emp.Id, pkg.Id, start: today.AddDays(5).AddHours(9)));
        await ctx.SaveChangesAsync();

        var repo = new BookingRepository(ctx);
        var result = (await repo.GetBookingsByDateRangeAsync(today, today.AddDays(1))).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    // ── GetBookingWithDetailsAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetBookingWithDetailsAsync_ReturnsFullGraph()
    {
        var (ctx, emp, cust, pkg) = await SetupBaseAsync();
        ctx.Bookings.Add(Seed.Booking(1, cust.Id, emp.Id, pkg.Id));
        await ctx.SaveChangesAsync();

        var repo = new BookingRepository(ctx);
        var booking = await repo.GetBookingWithDetailsAsync(1);

        Assert.NotNull(booking);
        Assert.NotNull(booking!.Customer);
        Assert.NotNull(booking.Employee);
        Assert.NotNull(booking.Package);
    }

    [Fact]
    public async Task GetBookingWithDetailsAsync_NotFound_ReturnsNull()
    {
        var (ctx, _, _, _) = await SetupBaseAsync();
        var repo = new BookingRepository(ctx);
        Assert.Null(await repo.GetBookingWithDetailsAsync(999));
    }

    // ── IsEmployeeAvailableAsync ─────────────────────────────────────────────

    [Fact]
    public async Task IsEmployeeAvailableAsync_NoConflict_ReturnsTrue()
    {
        var (ctx, emp, cust, pkg) = await SetupBaseAsync();
        var repo = new BookingRepository(ctx);

        var start = DateTime.UtcNow.AddDays(1);
        var result = await repo.IsEmployeeAvailableAsync(emp.Id, start, start.AddHours(1));

        Assert.True(result);
    }

    [Fact]
    public async Task IsEmployeeAvailableAsync_ConflictingBooking_ReturnsFalse()
    {
        var (ctx, emp, cust, pkg) = await SetupBaseAsync();
        var start = DateTime.UtcNow.AddDays(1);
        ctx.Bookings.Add(Seed.Booking(1, cust.Id, emp.Id, pkg.Id,
            status: BookingStatus.Confirmed, start: start));
        await ctx.SaveChangesAsync();

        var repo = new BookingRepository(ctx);
        var result = await repo.IsEmployeeAvailableAsync(emp.Id, start, start.AddHours(1));

        Assert.False(result);
    }

    [Fact]
    public async Task IsEmployeeAvailableAsync_CancelledBookingDoesNotBlock()
    {
        var (ctx, emp, cust, pkg) = await SetupBaseAsync();
        var start = DateTime.UtcNow.AddDays(1);
        ctx.Bookings.Add(Seed.Booking(1, cust.Id, emp.Id, pkg.Id,
            status: BookingStatus.Cancelled, start: start));
        await ctx.SaveChangesAsync();

        var repo = new BookingRepository(ctx);
        var result = await repo.IsEmployeeAvailableAsync(emp.Id, start, start.AddHours(1));

        Assert.True(result);
    }

    [Fact]
    public async Task IsEmployeeAvailableAsync_ExcludeBookingId_AllowsOwnBooking()
    {
        var (ctx, emp, cust, pkg) = await SetupBaseAsync();
        var start = DateTime.UtcNow.AddDays(1);
        ctx.Bookings.Add(Seed.Booking(5, cust.Id, emp.Id, pkg.Id,
            status: BookingStatus.Confirmed, start: start));
        await ctx.SaveChangesAsync();

        var repo = new BookingRepository(ctx);
        var result = await repo.IsEmployeeAvailableAsync(emp.Id, start, start.AddHours(1),
            excludeBookingId: 5);

        Assert.True(result);
    }
}
