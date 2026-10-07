using AutoShine.Models.Enums;
using AutoShine.Service.DTOs.Auth;
using AutoShine.Service.DTOs.Packages;
using AutoShine.Service.DTOs.Reviews;
using AutoShine.Service.DTOs.Schedules;
using AutoShine.Service.DTOs.Users;
using AutoShine.Service.Implementations;
using AutoShine.UnitTests.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AutoShine.UnitTests.ServiceTests;

/// <summary>Input validation and data-integrity rules across services.</summary>
public class ValidationAndDataTests
{
    private static IConfiguration JwtConfig() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "UnitTest_Secret_Key_That_Is_Long_Enough_123!",
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["Jwt:ExpiryHours"] = "1"
        })
        .Build();

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "Smith", "a@test.com", "Password1", "555")]
    [InlineData("Alice", "Smith", "not-an-email", "Password1", "555")]
    [InlineData("Alice", "Smith", "a@test.com", "short", "555")]
    [InlineData("Alice", "Smith", "a@test.com", "Password1", "")]
    public async Task Register_InvalidInput_Throws(string first, string last, string email, string password, string phone)
    {
        using var fx = new ServiceFixture();
        var svc = new AuthService(fx.Uow, JwtConfig());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.RegisterAsync(new RegisterDto(first, last, email, password, phone)));
    }

    [Fact]
    public async Task Register_ReturnsFirstAndLastName()
    {
        using var fx = new ServiceFixture();
        var svc = new AuthService(fx.Uow, JwtConfig());

        var result = await svc.RegisterAsync(new RegisterDto(" Alice ", "Smith", " A@Test.com ", "Password1", "555"));

        Assert.Equal("Alice", result.FirstName);
        Assert.Equal("Smith", result.LastName);
        Assert.Equal("a@test.com", result.Email);
    }

    // ── Users ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_SearchIsCaseInsensitive()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Employee(1, "Alice", "Smith"));
        var svc = new UserService(fx.Uow, fx.Mapper);

        var result = await svc.GetUsersAsync(1, 10, search: "aLiCe");

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetUsers_InvalidPaging_IsClamped()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Employee(1));
        var svc = new UserService(fx.Uow, fx.Mapper);

        var result = await svc.GetUsersAsync(page: 0, pageSize: 0);

        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.PageSize);
    }

    [Fact]
    public async Task UpdateUser_ChangesEmailAndRole()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(5));
        var svc = new UserService(fx.Uow, fx.Mapper);

        var result = await svc.UpdateUserAsync(5,
            new UpdateUserDto("Bob", "Jones", "New@Test.com", "555", UserRole.Employee, true));

        Assert.Equal("new@test.com", result!.Email);
        Assert.Equal("Employee", result.Role);
    }

    [Fact]
    public async Task UpdateUser_EmailTakenByAnotherUser_Throws()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(5), Seed.Customer(6));
        var svc = new UserService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateUserAsync(5,
            new UpdateUserDto("Bob", "Jones", "cust6@test.com", "555", UserRole.Customer, true)));
    }

    // ── Reviews ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReview_EmployeeIsTakenFromBooking()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Employee(1), Seed.Employee(2), Seed.Customer(100), Seed.Package(1));
        await fx.SeedAsync(Seed.Booking(1, 100, 2, 1, BookingStatus.Completed));
        var svc = new ReviewService(fx.Uow, fx.Mapper);

        var review = await svc.CreateReviewAsync(100, new CreateReviewDto(1, 5, "Great"));

        Assert.Equal(2, review.EmployeeId);
    }

    // ── Packages ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPackages_IncludingInactive_LoadsRecipeItems()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.InventoryItem(1), Seed.Package(1, active: false));
        fx.Ctx.PackageItems.Add(new() { PackageId = 1, InventoryItemId = 1, QuantityRequired = 2 });
        await fx.Ctx.SaveChangesAsync();
        fx.Ctx.ChangeTracker.Clear(); // behave like a fresh request
        var svc = new PackageService(fx.Uow, fx.Mapper);

        var pkg = (await svc.GetAllPackagesAsync(activeOnly: false)).Single();

        Assert.Single(pkg.Items);
        Assert.Equal("Item 1", pkg.Items[0].ItemName);
    }

    [Fact]
    public async Task CreatePackage_UnknownInventoryItem_Throws()
    {
        using var fx = new ServiceFixture();
        var svc = new PackageService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreatePackageAsync(
            new CreatePackageDto("Wash", "", 10m, 30, new() { new CreatePackageItemDto(42, 1) })));
    }

    [Fact]
    public async Task CreatePackage_RespectsIsActive()
    {
        using var fx = new ServiceFixture();
        var svc = new PackageService(fx.Uow, fx.Mapper);

        var pkg = await svc.CreatePackageAsync(new CreatePackageDto("Wash", "", 10m, 30, new(), IsActive: false));

        Assert.False(pkg.IsActive);
    }

    // ── Inventory ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteInventoryItem_UsedByPackage_ThrowsWithPackageName()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.InventoryItem(1), Seed.Package(1));
        fx.Ctx.PackageItems.Add(new() { PackageId = 1, InventoryItemId = 1, QuantityRequired = 1 });
        await fx.Ctx.SaveChangesAsync();
        var svc = new InventoryService(fx.Uow, fx.Mapper);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteItemAsync(1));
        Assert.Contains("Package 1", ex.Message);
    }

    // ── Schedules & profile ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateLeave_UnspecifiedDate_IsStoredAsUtcMidnight()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Employee(1));
        var svc = new ScheduleService(fx.Uow, fx.Mapper);

        var leave = await svc.CreateLeaveAsync(new CreateLeaveDto(1, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified), null));

        Assert.Equal(DateTimeKind.Utc, leave.Date.Kind);
        Assert.Equal(new DateTime(2026, 10, 1), leave.Date.Date);
    }

    [Fact]
    public async Task CreateLeave_Duplicate_Throws()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Employee(1));
        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        var dto = new CreateLeaveDto(1, new DateTime(2026, 10, 1), null);
        await svc.CreateLeaveAsync(dto);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateLeaveAsync(dto));
    }

    [Fact]
    public async Task CreateTemplate_ForNonEmployee_Throws()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(100));
        var svc = new ScheduleService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateTemplateAsync(new CreateScheduleTemplateDto(
            100, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), 62,
            new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), null, null)));
    }

    [Theory]
    [InlineData(2026, 12, 31, 2026, 1, 1, 9, 17, null, null)]   // end before start
    [InlineData(2026, 1, 1, 2026, 12, 31, 17, 9, null, null)]   // work end before start
    [InlineData(2026, 1, 1, 2026, 12, 31, 9, 17, 13, null)]     // half a break
    [InlineData(2026, 1, 1, 2026, 12, 31, 9, 17, 18, 19)]       // break outside hours
    public async Task CreateTemplate_InvalidInput_Throws(int sy, int sm, int sd, int ey, int em, int ed,
        int workStart, int workEnd, int? breakStart, int? breakEnd)
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Employee(1));
        var svc = new ScheduleService(fx.Uow, fx.Mapper);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateTemplateAsync(new CreateScheduleTemplateDto(
            1, new DateTime(sy, sm, sd), new DateTime(ey, em, ed), 62,
            TimeSpan.FromHours(workStart), TimeSpan.FromHours(workEnd),
            breakStart.HasValue ? TimeSpan.FromHours(breakStart.Value) : null,
            breakEnd.HasValue ? TimeSpan.FromHours(breakEnd.Value) : null)));
    }

    [Fact]
    public async Task UpdateProfile_NullPhone_StoresEmptyString()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(100));
        var svc = new ProfileService(fx.Uow);

        await svc.UpdateProfileAsync(100, new UpdateProfileDto("Bob", "Jones", null));

        Assert.Equal(string.Empty, (await fx.Uow.Users.GetByIdAsync(100))!.Phone);
    }
}
