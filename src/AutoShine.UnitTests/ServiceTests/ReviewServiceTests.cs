using AutoShine.Models.Enums;
using AutoShine.Service.DTOs.Reviews;
using AutoShine.Service.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.ServiceTests;

public class ReviewServiceTests
{
    private static async Task<ServiceFixture> BuildFixtureAsync()
    {
        var fx = new ServiceFixture();
        var emp = Seed.Employee(1);
        var cust = Seed.Customer(100);
        var pkg = Seed.Package(1);
        var booking = Seed.Booking(1, cust.Id, emp.Id, pkg.Id,
            BookingStatus.Completed);
        await fx.SeedAsync(emp, cust, pkg, booking);
        return fx;
    }

    // ── CreateReviewAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReviewAsync_ValidCompletedBooking_ReturnsDto()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ReviewService(fx.Uow, fx.Mapper);

        var dto = new CreateReviewDto(BookingId: 1, EmployeeId: 1, Rating: 5, Comment: "Great!");
        var result = await svc.CreateReviewAsync(customerId: 100, dto);

        Assert.Equal(5, result.Rating);
        Assert.Equal("Great!", result.Comment);
        Assert.NotEmpty(result.CustomerName);
        Assert.NotEmpty(result.EmployeeName);
    }

    [Fact]
    public async Task CreateReviewAsync_BookingNotFound_Throws()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ReviewService(fx.Uow, fx.Mapper);

        var dto = new CreateReviewDto(BookingId: 999, EmployeeId: 1, Rating: 4, Comment: null);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.CreateReviewAsync(100, dto));
    }

    [Fact]
    public async Task CreateReviewAsync_WrongCustomer_Throws()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ReviewService(fx.Uow, fx.Mapper);

        var dto = new CreateReviewDto(BookingId: 1, EmployeeId: 1, Rating: 3, Comment: null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.CreateReviewAsync(customerId: 999 /* not the owner */, dto));
    }

    [Fact]
    public async Task CreateReviewAsync_BookingNotCompleted_Throws()
    {
        using var fx = new ServiceFixture();
        var emp = Seed.Employee(1);
        var cust = Seed.Customer(100);
        var pkg = Seed.Package(1);
        // Pending booking — not completed
        var booking = Seed.Booking(1, cust.Id, emp.Id, pkg.Id, BookingStatus.Pending);
        await fx.SeedAsync(emp, cust, pkg, booking);

        var svc = new ReviewService(fx.Uow, fx.Mapper);
        var dto = new CreateReviewDto(BookingId: 1, EmployeeId: 1, Rating: 5, Comment: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateReviewAsync(100, dto));
    }

    [Fact]
    public async Task CreateReviewAsync_DuplicateReview_Throws()
    {
        using var fx = await BuildFixtureAsync();
        // Seed an existing review
        var existingReview = Seed.Review(1, bookingId: 1, customerId: 100, employeeId: 1);
        await fx.SeedAsync(existingReview);

        var svc = new ReviewService(fx.Uow, fx.Mapper);
        var dto = new CreateReviewDto(BookingId: 1, EmployeeId: 1, Rating: 4, Comment: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateReviewAsync(100, dto));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task CreateReviewAsync_InvalidRating_Throws(int rating)
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ReviewService(fx.Uow, fx.Mapper);
        var dto = new CreateReviewDto(BookingId: 1, EmployeeId: 1, Rating: rating, Comment: null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateReviewAsync(100, dto));
    }

    // ── GetReviewsByEmployeeAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetReviewsByEmployeeAsync_ReturnsMatchingReviews()
    {
        using var fx = await BuildFixtureAsync();
        await fx.SeedAsync(Seed.Review(1, bookingId: 1, customerId: 100, employeeId: 1, rating: 5));

        var svc = new ReviewService(fx.Uow, fx.Mapper);
        var reviews = (await svc.GetReviewsByEmployeeAsync(employeeId: 1)).ToList();

        Assert.Single(reviews);
        Assert.Equal(5, reviews[0].Rating);
        Assert.Equal(1, reviews[0].EmployeeId);
    }

    [Fact]
    public async Task GetReviewsByEmployeeAsync_NoReviews_ReturnsEmpty()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ReviewService(fx.Uow, fx.Mapper);
        Assert.Empty(await svc.GetReviewsByEmployeeAsync(employeeId: 99));
    }

    // ── GetReviewByBookingAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetReviewByBookingAsync_Exists_ReturnsDto()
    {
        using var fx = await BuildFixtureAsync();
        await fx.SeedAsync(Seed.Review(1, bookingId: 1, customerId: 100, employeeId: 1));

        var svc = new ReviewService(fx.Uow, fx.Mapper);
        var result = await svc.GetReviewByBookingAsync(bookingId: 1);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetReviewByBookingAsync_NotFound_ReturnsNull()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ReviewService(fx.Uow, fx.Mapper);
        Assert.Null(await svc.GetReviewByBookingAsync(bookingId: 999));
    }

    // ── DeleteReviewAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteReviewAsync_ExistingReview_ReturnsTrueAndDeletes()
    {
        using var fx = await BuildFixtureAsync();
        await fx.SeedAsync(Seed.Review(1, bookingId: 1, customerId: 100, employeeId: 1));

        var svc = new ReviewService(fx.Uow, fx.Mapper);
        var result = await svc.DeleteReviewAsync(reviewId: 1);

        Assert.True(result);
        Assert.Null(await svc.GetReviewByBookingAsync(1));
    }

    [Fact]
    public async Task DeleteReviewAsync_NotFound_ReturnsFalse()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ReviewService(fx.Uow, fx.Mapper);
        Assert.False(await svc.DeleteReviewAsync(reviewId: 999));
    }
}
