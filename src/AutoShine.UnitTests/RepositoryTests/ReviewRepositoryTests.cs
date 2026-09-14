using AutoShine.Repository.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.RepositoryTests;

public class ReviewRepositoryTests
{
    private static async Task<(AutoShine.Data.AppDbContext ctx,
        AutoShine.Models.Entities.User emp,
        AutoShine.Models.Entities.User cust,
        AutoShine.Models.Entities.Booking booking)> SetupAsync()
    {
        var ctx = DbContextFactory.Create();
        var emp = Seed.Employee(1);
        var cust = Seed.Customer(100);
        var pkg = Seed.Package(1);
        ctx.Users.AddRange(emp, cust);
        ctx.Packages.Add(pkg);
        var booking = Seed.Booking(1, cust.Id, emp.Id, pkg.Id,
            AutoShine.Models.Enums.BookingStatus.Completed);
        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync();
        return (ctx, emp, cust, booking);
    }

    // ── GetReviewsByEmployeeAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetReviewsByEmployeeAsync_ReturnsReviewsWithNavigations()
    {
        var (ctx, emp, cust, booking) = await SetupAsync();
        ctx.Reviews.Add(Seed.Review(1, booking.Id, cust.Id, emp.Id));
        await ctx.SaveChangesAsync();

        var repo = new ReviewRepository(ctx);
        var reviews = (await repo.GetReviewsByEmployeeAsync(emp.Id)).ToList();

        Assert.Single(reviews);
        Assert.NotNull(reviews[0].Customer);
        Assert.NotNull(reviews[0].Employee);
    }

    [Fact]
    public async Task GetReviewsByEmployeeAsync_OnlyReturnsForEmployee()
    {
        var (ctx, emp, cust, booking) = await SetupAsync();
        var emp2 = Seed.Employee(2, "Dave", "Brown");
        ctx.Users.Add(emp2);
        var pkg2 = Seed.Package(2);
        ctx.Packages.Add(pkg2);
        var booking2 = Seed.Booking(2, cust.Id, emp2.Id, pkg2.Id,
            AutoShine.Models.Enums.BookingStatus.Completed);
        ctx.Bookings.Add(booking2);
        ctx.Reviews.AddRange(
            Seed.Review(1, booking.Id, cust.Id, emp.Id, rating: 5),
            Seed.Review(2, booking2.Id, cust.Id, emp2.Id, rating: 3));
        await ctx.SaveChangesAsync();

        var repo = new ReviewRepository(ctx);
        var reviews = (await repo.GetReviewsByEmployeeAsync(emp.Id)).ToList();

        Assert.Single(reviews);
        Assert.Equal(emp.Id, reviews[0].EmployeeId);
    }

    [Fact]
    public async Task GetReviewsByEmployeeAsync_NoReviews_ReturnsEmpty()
    {
        var (ctx, emp, _, _) = await SetupAsync();
        var repo = new ReviewRepository(ctx);
        Assert.Empty(await repo.GetReviewsByEmployeeAsync(emp.Id));
    }

    // ── GetReviewByBookingAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetReviewByBookingAsync_ExistingReview_ReturnsWithNavigations()
    {
        var (ctx, emp, cust, booking) = await SetupAsync();
        ctx.Reviews.Add(Seed.Review(1, booking.Id, cust.Id, emp.Id));
        await ctx.SaveChangesAsync();

        var repo = new ReviewRepository(ctx);
        var review = await repo.GetReviewByBookingAsync(booking.Id);

        Assert.NotNull(review);
        Assert.NotNull(review!.Customer);
        Assert.NotNull(review.Employee);
    }

    [Fact]
    public async Task GetReviewByBookingAsync_NoReview_ReturnsNull()
    {
        var (ctx, _, _, booking) = await SetupAsync();
        var repo = new ReviewRepository(ctx);
        Assert.Null(await repo.GetReviewByBookingAsync(booking.Id));
    }

    // ── GetAverageRatingForEmployeeAsync ────────────────────────────────────

    [Fact]
    public async Task GetAverageRating_WithReviews_ReturnsCorrectAverage()
    {
        var (ctx, emp, cust, booking) = await SetupAsync();
        var pkg2 = Seed.Package(2);
        ctx.Packages.Add(pkg2);
        var booking2 = Seed.Booking(2, cust.Id, emp.Id, pkg2.Id,
            AutoShine.Models.Enums.BookingStatus.Completed);
        ctx.Bookings.Add(booking2);
        ctx.Reviews.AddRange(
            Seed.Review(1, booking.Id, cust.Id, emp.Id, rating: 4),
            Seed.Review(2, booking2.Id, cust.Id, emp.Id, rating: 2));
        await ctx.SaveChangesAsync();

        var repo = new ReviewRepository(ctx);
        var avg = await repo.GetAverageRatingForEmployeeAsync(emp.Id);

        Assert.Equal(3.0, avg);
    }

    [Fact]
    public async Task GetAverageRating_NoReviews_ReturnsZero()
    {
        var (ctx, emp, _, _) = await SetupAsync();
        var repo = new ReviewRepository(ctx);
        Assert.Equal(0.0, await repo.GetAverageRatingForEmployeeAsync(emp.Id));
    }

    // ── AddAsync / Remove ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddAndRemoveReview_WorksCorrectly()
    {
        var (ctx, emp, cust, booking) = await SetupAsync();
        var review = Seed.Review(1, booking.Id, cust.Id, emp.Id);
        ctx.Reviews.Add(review);
        await ctx.SaveChangesAsync();

        var repo = new ReviewRepository(ctx);
        repo.Remove(review);
        await ctx.SaveChangesAsync();

        Assert.Null(await repo.GetReviewByBookingAsync(booking.Id));
    }
}
