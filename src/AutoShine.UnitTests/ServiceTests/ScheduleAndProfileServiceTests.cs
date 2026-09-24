using AutoShine.Service.DTOs.Schedules;
using AutoShine.Service.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.ServiceTests;

public class ScheduleServiceTests
{
    private static async Task<ServiceFixture> BuildFixtureAsync()
    {
        var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Employee(1));
        return fx;
    }

    // ── CreateTemplateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateTemplateAsync_ValidData_ReturnsDto()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ScheduleService(fx.Uow, fx.Mapper);

        var dto = new CreateScheduleTemplateDto(
            EmployeeId: 1,
            StartDate: DateTime.UtcNow.Date,
            EndDate: DateTime.UtcNow.Date.AddDays(30),
            WorkingDays: 62, // Mon-Fri
            WorkStartTime: new TimeSpan(9, 0, 0),
            WorkEndTime: new TimeSpan(17, 0, 0),
            BreakStartTime: null,
            BreakEndTime: null);

        var result = await svc.CreateTemplateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(1, result.EmployeeId);
        Assert.Equal("Alice Smith", result.EmployeeName);
        Assert.Equal(62, result.WorkingDays);
    }

    // ── UpdateTemplateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTemplateAsync_ExistingTemplate_UpdatesFields()
    {
        using var fx = await BuildFixtureAsync();
        fx.Ctx.EmployeeScheduleTemplates.Add(Seed.Template(1, employeeId: 1));
        await fx.Ctx.SaveChangesAsync();

        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        var dto = new CreateScheduleTemplateDto(
            EmployeeId: 1,
            StartDate: DateTime.UtcNow.Date,
            EndDate: DateTime.UtcNow.Date.AddDays(60),
            WorkingDays: 2, // Monday only
            WorkStartTime: new TimeSpan(8, 0, 0),
            WorkEndTime: new TimeSpan(16, 0, 0),
            BreakStartTime: null,
            BreakEndTime: null);

        var result = await svc.UpdateTemplateAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal(2, result!.WorkingDays);
        Assert.Equal(new TimeSpan(8, 0, 0), result.WorkStartTime);
    }

    [Fact]
    public async Task UpdateTemplateAsync_NotFound_ReturnsNull()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        var dto = new CreateScheduleTemplateDto(1, DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(1), 62,
            new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), null, null);

        Assert.Null(await svc.UpdateTemplateAsync(999, dto));
    }

    // ── DeleteTemplateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTemplateAsync_ExistingTemplate_DeletesAndReturnsTrue()
    {
        using var fx = await BuildFixtureAsync();
        fx.Ctx.EmployeeScheduleTemplates.Add(Seed.Template(1, employeeId: 1));
        await fx.Ctx.SaveChangesAsync();

        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        Assert.True(await svc.DeleteTemplateAsync(1));
        Assert.Null(await fx.Uow.Schedules.GetTemplateByIdAsync(1));
    }

    [Fact]
    public async Task DeleteTemplateAsync_NotFound_ReturnsFalse()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        Assert.False(await svc.DeleteTemplateAsync(999));
    }

    // ── GetTemplatesByEmployeeAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetTemplatesByEmployeeAsync_ReturnsActiveOnly()
    {
        using var fx = await BuildFixtureAsync();
        var active = Seed.Template(1, employeeId: 1);
        var inactive = Seed.Template(2, employeeId: 1); inactive.IsActive = false;
        fx.Ctx.EmployeeScheduleTemplates.AddRange(active, inactive);
        await fx.Ctx.SaveChangesAsync();

        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        var result = (await svc.GetTemplatesByEmployeeAsync(1)).ToList();

        Assert.Single(result);
    }

    // ── GetAllTemplatesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllTemplatesAsync_ReturnsTemplatesFromAllEmployees()
    {
        using var fx = new ServiceFixture();
        var emp1 = Seed.Employee(1);
        var emp2 = Seed.Employee(2, "Dave", "Brown");
        await fx.SeedAsync(emp1, emp2);
        fx.Ctx.EmployeeScheduleTemplates.AddRange(
            Seed.Template(1, employeeId: 1),
            Seed.Template(2, employeeId: 2));
        await fx.Ctx.SaveChangesAsync();

        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        var result = (await svc.GetAllTemplatesAsync()).ToList();

        Assert.Equal(2, result.Count);
    }

    // ── CreateLeaveAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLeaveAsync_ValidData_ReturnsDto()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ScheduleService(fx.Uow, fx.Mapper);

        var dto = new CreateLeaveDto(EmployeeId: 1, Date: DateTime.UtcNow.Date, Reason: "Sick");
        var result = await svc.CreateLeaveAsync(dto);

        Assert.Equal(1, result.EmployeeId);
        Assert.Equal("Alice Smith", result.EmployeeName);
        Assert.Equal("Sick", result.Reason);
    }

    // ── DeleteLeaveAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteLeaveAsync_ExistingLeave_DeletesAndReturnsTrue()
    {
        using var fx = await BuildFixtureAsync();
        fx.Ctx.EmployeeLeaves.Add(Seed.Leave(1, employeeId: 1));
        await fx.Ctx.SaveChangesAsync();

        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        Assert.True(await svc.DeleteLeaveAsync(1));
        Assert.Null(await fx.Uow.Schedules.GetLeaveByIdAsync(1));
    }

    [Fact]
    public async Task DeleteLeaveAsync_NotFound_ReturnsFalse()
    {
        using var fx = await BuildFixtureAsync();
        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        Assert.False(await svc.DeleteLeaveAsync(999));
    }

    // ── GetAllLeavesAsync / GetLeavesByEmployeeAsync ───────────────────────────

    [Fact]
    public async Task GetAllLeavesAsync_ReturnsBothEmployeesLeaves()
    {
        using var fx = new ServiceFixture();
        var emp1 = Seed.Employee(1); var emp2 = Seed.Employee(2, "Dave", "Brown");
        await fx.SeedAsync(emp1, emp2);
        fx.Ctx.EmployeeLeaves.AddRange(
            Seed.Leave(1, 1, DateTime.UtcNow.Date),
            Seed.Leave(2, 2, DateTime.UtcNow.Date));
        await fx.Ctx.SaveChangesAsync();

        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        Assert.Equal(2, (await svc.GetAllLeavesAsync()).Count());
    }

    [Fact]
    public async Task GetLeavesByEmployeeAsync_OnlyForThatEmployee()
    {
        using var fx = new ServiceFixture();
        var emp1 = Seed.Employee(1); var emp2 = Seed.Employee(2, "Dave", "Brown");
        await fx.SeedAsync(emp1, emp2);
        fx.Ctx.EmployeeLeaves.AddRange(
            Seed.Leave(1, 1, DateTime.UtcNow.Date),
            Seed.Leave(2, 2, DateTime.UtcNow.Date));
        await fx.Ctx.SaveChangesAsync();

        var svc = new ScheduleService(fx.Uow, fx.Mapper);
        var result = (await svc.GetLeavesByEmployeeAsync(1)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].EmployeeId);
    }
}

public class ProfileServiceTests
{
    // ── GetProfileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_Found_ReturnsDto()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1));

        var svc = new ProfileService(fx.Uow);
        var result = await svc.GetProfileAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Bob", result!.FirstName);
    }

    [Fact]
    public async Task GetProfileAsync_NotFound_ReturnsNull()
    {
        using var fx = new ServiceFixture();
        var svc = new ProfileService(fx.Uow);
        Assert.Null(await svc.GetProfileAsync(999));
    }

    // ── UpdateProfileAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfileAsync_UpdatesNameAndPhone()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1));

        var svc = new ProfileService(fx.Uow);
        var dto = new AutoShine.Service.DTOs.Schedules.UpdateProfileDto("NewFirst", "NewLast", "111-2222");
        var result = await svc.UpdateProfileAsync(1, dto);

        Assert.Equal("NewFirst", result!.FirstName);
        Assert.Equal("NewLast", result.LastName);
    }

    [Fact]
    public async Task UpdateProfileAsync_TrimsWhitespace()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Customer(1));

        var svc = new ProfileService(fx.Uow);
        var dto = new AutoShine.Service.DTOs.Schedules.UpdateProfileDto("  Alice  ", "  Smith  ", null);
        var result = await svc.UpdateProfileAsync(1, dto);

        Assert.Equal("Alice", result!.FirstName);
        Assert.Equal("Smith", result.LastName);
    }

    [Fact]
    public async Task UpdateProfileAsync_NotFound_ReturnsNull()
    {
        using var fx = new ServiceFixture();
        var svc = new ProfileService(fx.Uow);
        var dto = new AutoShine.Service.DTOs.Schedules.UpdateProfileDto("X", "Y", null);
        Assert.Null(await svc.UpdateProfileAsync(999, dto));
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_ChangesHash()
    {
        using var fx = new ServiceFixture();
        var user = Seed.Customer(1);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!");
        await fx.SeedAsync(user);

        var svc = new ProfileService(fx.Uow);
        var dto = new AutoShine.Service.DTOs.Schedules.ChangePasswordDto("OldPass1!", "NewPass99!");
        Assert.True(await svc.ChangePasswordAsync(1, dto));

        var updated = await fx.Uow.Users.GetByIdAsync(1);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass99!", updated!.PasswordHash));
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_Throws()
    {
        using var fx = new ServiceFixture();
        var user = Seed.Customer(1);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!");
        await fx.SeedAsync(user);

        var svc = new ProfileService(fx.Uow);
        var dto = new AutoShine.Service.DTOs.Schedules.ChangePasswordDto("WrongPass", "NewPass99!");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.ChangePasswordAsync(1, dto));
    }

    [Fact]
    public async Task ChangePasswordAsync_NewPasswordTooShort_Throws()
    {
        using var fx = new ServiceFixture();
        var user = Seed.Customer(1);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!");
        await fx.SeedAsync(user);

        var svc = new ProfileService(fx.Uow);
        var dto = new AutoShine.Service.DTOs.Schedules.ChangePasswordDto("OldPass1!", "short");
        await Assert.ThrowsAsync<ArgumentException>(() => svc.ChangePasswordAsync(1, dto));
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ReturnsFalse()
    {
        using var fx = new ServiceFixture();
        var svc = new ProfileService(fx.Uow);
        var dto = new AutoShine.Service.DTOs.Schedules.ChangePasswordDto("x", "y");
        Assert.False(await svc.ChangePasswordAsync(999, dto));
    }
}
