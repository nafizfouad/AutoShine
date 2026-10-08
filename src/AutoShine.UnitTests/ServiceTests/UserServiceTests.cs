using AutoShine.Models.Enums;
using AutoShine.Service.DTOs.Users;
using AutoShine.Service.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.ServiceTests;

public class UserServiceTests
{
    // ── GetUsersAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_NoFilter_ReturnsAllUsers()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1), Seed.Employee(2), Seed.Admin(200));

        var svc = new UserService(fx.Uow, fx.Mapper);
        var result = await svc.GetUsersAsync(1, 10);

        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetUsersAsync_RoleFilter_ReturnsOnlyMatchingRole()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1), Seed.Employee(2), Seed.Admin(200));

        var svc = new UserService(fx.Uow, fx.Mapper);
        var result = await svc.GetUsersAsync(1, 10, role: "Employee");

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, u => Assert.Equal("Employee", u.Role));
    }

    [Fact]
    public async Task GetUsersAsync_InvalidRole_ReturnsAllUsers()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1), Seed.Employee(2));

        var svc = new UserService(fx.Uow, fx.Mapper);
        var result = await svc.GetUsersAsync(1, 10, role: "NotARole");

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetUsersAsync_SearchFilter_MatchesByName()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1, "Alice", "Smith"), Seed.Customer(2, "Bob", "Jones"));

        var svc = new UserService(fx.Uow, fx.Mapper);
        var result = await svc.GetUsersAsync(1, 10, search: "Alice");

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetUsersAsync_SearchFilter_MatchesByEmail()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1));

        var svc = new UserService(fx.Uow, fx.Mapper);
        var result = await svc.GetUsersAsync(1, 10, search: "cust1");

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetUsersAsync_Pagination_ReturnsCorrectPage()
    {
        using var fx = new ServiceFixture();
        for (int i = 1; i <= 15; i++) fx.Ctx.Users.Add(Seed.Customer(i));
        await fx.Ctx.SaveChangesAsync();

        var svc = new UserService(fx.Uow, fx.Mapper);
        var result = await svc.GetUsersAsync(page: 2, pageSize: 5);

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(5, result.Items.Count());
        Assert.Equal(2, result.Page);
    }

    // ── GetUserByIdAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_Found_ReturnsDto()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(42));

        var svc = new UserService(fx.Uow, fx.Mapper);
        var dto = await svc.GetUserByIdAsync(42);

        Assert.NotNull(dto);
        Assert.Equal(42, dto!.Id);
        Assert.Equal("Customer", dto.Role);
    }

    [Fact]
    public async Task GetUserByIdAsync_NotFound_ReturnsNull()
    {
        using var fx = new ServiceFixture();
        var svc = new UserService(fx.Uow, fx.Mapper);
        Assert.Null(await svc.GetUserByIdAsync(999));
    }

    // ── CreateUserAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_NewEmail_PersistsAndReturnsDto()
    {
        using var fx = new ServiceFixture();
        var svc = new UserService(fx.Uow, fx.Mapper);
        var dto = new CreateUserDto("Carol", "White", "carol@test.com", "P@ssw0rd1!", "555-0003", UserRole.Employee);

        var result = await svc.CreateUserAsync(dto);

        Assert.Equal("carol@test.com", result.Email);
        Assert.Equal("Employee", result.Role);
        // Verify in DB
        Assert.NotNull(await fx.Uow.Users.GetByEmailAsync("carol@test.com"));
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_Throws()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1));
        var svc = new UserService(fx.Uow, fx.Mapper);
        var dto = new CreateUserDto("X", "Y", "cust1@test.com", "Password1", "x", UserRole.Customer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateUserAsync(dto));
    }

    // ── UpdateUserAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_ExistingUser_UpdatesFields()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(5));
        var svc = new UserService(fx.Uow, fx.Mapper);

        var dto = new UpdateUserDto("Updated", "Name", "cust5@test.com", "555-9999", UserRole.Customer, true);
        var result = await svc.UpdateUserAsync(5, dto);

        Assert.NotNull(result);
        Assert.Equal("Updated", result!.FirstName);
        Assert.Equal("Name", result.LastName);
    }

    [Fact]
    public async Task UpdateUserAsync_CanDeactivateUser()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(6));
        var svc = new UserService(fx.Uow, fx.Mapper);

        var result = await svc.UpdateUserAsync(6,
            new UpdateUserDto("Bob", "Jones", "cust6@test.com", "555-0002", UserRole.Customer, false));

        Assert.False(result!.IsActive);
    }

    [Fact]
    public async Task UpdateUserAsync_NotFound_ReturnsNull()
    {
        using var fx = new ServiceFixture();
        var svc = new UserService(fx.Uow, fx.Mapper);
        Assert.Null(await svc.UpdateUserAsync(999, new UpdateUserDto("X", "Y", "x@test.com", "z", UserRole.Customer, true)));
    }

    // ── DeleteUserAsync (soft delete) ─────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_ExistingUser_SetsIsActiveFalse()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(7));
        var svc = new UserService(fx.Uow, fx.Mapper);

        var result = await svc.DeleteUserAsync(7);

        Assert.True(result);
        var user = await fx.Uow.Users.GetByIdAsync(7);
        Assert.False(user!.IsActive);
    }

    [Fact]
    public async Task DeleteUserAsync_NotFound_ReturnsFalse()
    {
        using var fx = new ServiceFixture();
        var svc = new UserService(fx.Uow, fx.Mapper);
        Assert.False(await svc.DeleteUserAsync(999));
    }
}
