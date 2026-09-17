using AutoShine.Models.Enums;
using AutoShine.Service.DTOs.Auth;
using AutoShine.Service.Implementations;
using AutoShine.UnitTests.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AutoShine.UnitTests.ServiceTests;

public class AuthServiceTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddJsonStream(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("""
            {
              "Jwt": {
                "SecretKey": "supersecret_testkey_at_least_32chars_long!",
                "Issuer": "AutoShine",
                "Audience": "AutoShineUsers",
                "ExpiryHours": "24"
              }
            }
            """)))
            .Build();


    // ── RegisterAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesCustomerAndReturnsToken()
    {
        using var fx = new ServiceFixture();
        var svc = new AuthService(fx.Uow, BuildConfig());

        var dto = new RegisterDto("Alice", "Smith", "alice@test.com", "Pass1234!", "555-0001");
        var response = await svc.RegisterAsync(dto);

        Assert.NotEmpty(response.Token);
        Assert.Equal("Customer", response.Role);
        Assert.Equal("alice@test.com", response.Email);

        // Verify user persisted
        var user = await fx.Uow.Users.GetByEmailAsync("alice@test.com");
        Assert.NotNull(user);
        Assert.Equal(UserRole.Customer, user!.Role);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_Throws()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1, "Bob", "Jones"));

        var svc = new AuthService(fx.Uow, BuildConfig());
        var dto = new RegisterDto("Bob2", "Jones2", "cust1@test.com", "Pass1234!", "555-0002");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RegisterAsync(dto));
    }

    // ── LoginAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsToken()
    {
        using var fx = new ServiceFixture();
        var svc = new AuthService(fx.Uow, BuildConfig());

        // Register first so the password hash is correct
        await svc.RegisterAsync(new RegisterDto("Alice", "S", "a@test.com", "P@ssw0rd!", "x"));
        var response = await svc.LoginAsync(new LoginDto("a@test.com", "P@ssw0rd!"));

        Assert.NotEmpty(response.Token);
        Assert.Equal("Customer", response.Role);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_Throws()
    {
        using var fx = new ServiceFixture();
        var svc = new AuthService(fx.Uow, BuildConfig());
        await svc.RegisterAsync(new RegisterDto("Alice", "S", "a@test.com", "P@ssw0rd!", "x"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.LoginAsync(new LoginDto("a@test.com", "WrongPassword")));
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_Throws()
    {
        using var fx = new ServiceFixture();
        var svc = new AuthService(fx.Uow, BuildConfig());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.LoginAsync(new LoginDto("nobody@test.com", "pass")));
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccount_Throws()
    {
        using var fx = new ServiceFixture();
        var svc = new AuthService(fx.Uow, BuildConfig());
        await svc.RegisterAsync(new RegisterDto("Alice", "S", "a@test.com", "P@ssw0rd!", "x"));

        // Deactivate the user directly
        var user = await fx.Uow.Users.GetByEmailAsync("a@test.com");
        user!.IsActive = false;
        fx.Uow.Users.Update(user);
        await fx.Uow.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.LoginAsync(new LoginDto("a@test.com", "P@ssw0rd!")));
    }

    // ── Token contains correct claims ────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ReturnedToken_ContainsUserInfo()
    {
        using var fx = new ServiceFixture();
        var svc = new AuthService(fx.Uow, BuildConfig());
        var response = await svc.RegisterAsync(
            new RegisterDto("Alice", "Smith", "alice@test.com", "Pass1234!", "555-0001"));

        Assert.Equal("alice@test.com", response.Email);
        Assert.Equal("Alice Smith", response.FullName);
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
    }
}
