using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.RepositoryTests;

public class UserRepositoryTests
{
    // ── GetByEmailAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Users.Add(Seed.Customer(1, "Bob", "Jones"));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var user = await repo.GetByEmailAsync("cust1@test.com");

        Assert.NotNull(user);
        Assert.Equal(1, user!.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_CaseInsensitive_ReturnsUser()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Users.Add(Seed.Customer(2));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var user = await repo.GetByEmailAsync("CUST2@TEST.COM");

        Assert.NotNull(user);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistentEmail_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create();
        var repo = new UserRepository(ctx);
        var user = await repo.GetByEmailAsync("nobody@test.com");
        Assert.Null(user);
    }

    // ── GetByRoleAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByRoleAsync_ReturnsOnlyActiveUsersOfRole()
    {
        using var ctx = DbContextFactory.Create();
        var activeEmp = Seed.Employee(1);
        var inactiveEmp = Seed.Employee(2); inactiveEmp.IsActive = false;
        var customer = Seed.Customer(100);
        ctx.Users.AddRange(activeEmp, inactiveEmp, customer);
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var employees = (await repo.GetByRoleAsync(UserRole.Employee)).ToList();

        Assert.Single(employees);
        Assert.Equal(1, employees[0].Id);
    }

    [Fact]
    public async Task GetByRoleAsync_NoMatchingRole_ReturnsEmpty()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Users.Add(Seed.Customer(100));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var result = await repo.GetByRoleAsync(UserRole.Admin);
        Assert.Empty(result);
    }

    // ── Generic: GetByIdAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsUser()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Users.Add(Seed.Employee(5));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var user = await repo.GetByIdAsync(5);

        Assert.NotNull(user);
        Assert.Equal("Alice", user!.FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create();
        var repo = new UserRepository(ctx);
        var user = await repo.GetByIdAsync(999);
        Assert.Null(user);
    }

    // ── Generic: AddAsync / GetAllAsync ─────────────────────────────────────

    [Fact]
    public async Task AddAsync_ThenGetAll_ContainsUser()
    {
        using var ctx = DbContextFactory.Create();
        var repo = new UserRepository(ctx);
        await repo.AddAsync(Seed.Customer(3));
        await ctx.SaveChangesAsync();

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Single(all);
    }

    // ── Generic: Update / Remove ─────────────────────────────────────────────

    [Fact]
    public async Task Update_ChangesPersistedAfterSave()
    {
        using var ctx = DbContextFactory.Create();
        var user = Seed.Customer(4);
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        user.FirstName = "Charlie";
        repo.Update(user);
        await ctx.SaveChangesAsync();

        var fetched = await repo.GetByIdAsync(4);
        Assert.Equal("Charlie", fetched!.FirstName);
    }

    [Fact]
    public async Task Remove_DeletesUserFromDatabase()
    {
        using var ctx = DbContextFactory.Create();
        var user = Seed.Customer(5);
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        repo.Remove(user);
        await ctx.SaveChangesAsync();

        Assert.Null(await repo.GetByIdAsync(5));
    }

    // ── Generic: GetPagedAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        using var ctx = DbContextFactory.Create();
        for (int i = 1; i <= 15; i++) ctx.Users.Add(Seed.Customer(i));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var (items, total) = await repo.GetPagedAsync(page: 2, pageSize: 5);

        Assert.Equal(15, total);
        Assert.Equal(5, items.Count());
    }

    [Fact]
    public async Task GetPagedAsync_WithFilter_ReturnsFilteredCount()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Users.AddRange(Seed.Employee(1), Seed.Customer(100), Seed.Customer(101));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var (items, total) = await repo.GetPagedAsync(
            page: 1, pageSize: 10,
            filter: u => u.Role == UserRole.Customer);

        Assert.Equal(2, total);
        Assert.All(items, u => Assert.Equal(UserRole.Customer, u.Role));
    }

    // ── Generic: AnyAsync / CountAsync / FindAsync ───────────────────────────

    [Fact]
    public async Task AnyAsync_ReturnsTrue_WhenMatchExists()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Users.Add(Seed.Customer(10));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        Assert.True(await repo.AnyAsync(u => u.Role == UserRole.Customer));
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Users.AddRange(Seed.Customer(20), Seed.Customer(21));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        Assert.Equal(2, await repo.CountAsync());
        Assert.Equal(0, await repo.CountAsync(u => u.Role == UserRole.Admin));
    }

    [Fact]
    public async Task FindAsync_ReturnsOnlyMatchingUsers()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Users.AddRange(Seed.Employee(1), Seed.Customer(100));
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx);
        var result = await repo.FindAsync(u => u.Role == UserRole.Employee);
        Assert.Single(result);
    }
}
