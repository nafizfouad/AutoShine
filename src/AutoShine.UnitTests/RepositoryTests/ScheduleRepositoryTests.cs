using AutoShine.Models.Entities;
using AutoShine.Repository.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.RepositoryTests;

public class ScheduleRepositoryTests
{
    private static async Task<(AutoShine.Data.AppDbContext ctx, User emp)> SetupAsync()
    {
        var ctx = DbContextFactory.Create();
        var emp = Seed.Employee(1);
        ctx.Users.Add(emp);
        await ctx.SaveChangesAsync();
        return (ctx, emp);
    }

    // ── Templates ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTemplatesByEmployeeAsync_ReturnsOnlyActiveTemplates()
    {
        var (ctx, emp) = await SetupAsync();
        var active = Seed.Template(1, emp.Id);
        var inactive = Seed.Template(2, emp.Id); inactive.IsActive = false;
        ctx.EmployeeScheduleTemplates.AddRange(active, inactive);
        await ctx.SaveChangesAsync();

        var repo = new ScheduleRepository(ctx);
        var result = (await repo.GetTemplatesByEmployeeAsync(emp.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetTemplatesByEmployeeAsync_OnlyForThatEmployee()
    {
        var (ctx, emp) = await SetupAsync();
        var emp2 = Seed.Employee(2, "Dave", "Brown");
        ctx.Users.Add(emp2);
        ctx.EmployeeScheduleTemplates.AddRange(
            Seed.Template(1, emp.Id),
            Seed.Template(2, emp2.Id));
        await ctx.SaveChangesAsync();

        var repo = new ScheduleRepository(ctx);
        var result = (await repo.GetTemplatesByEmployeeAsync(emp.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(emp.Id, result[0].EmployeeId);
    }

    [Fact]
    public async Task GetActiveTemplatesForDateAsync_ReturnsTemplatesContainingDate()
    {
        var (ctx, emp) = await SetupAsync();
        var today = DateTime.UtcNow.Date;
        var covering = Seed.Template(1, emp.Id);
        covering.StartDate = today.AddDays(-5);
        covering.EndDate = today.AddDays(5);

        var notCovering = Seed.Template(2, emp.Id);
        notCovering.StartDate = today.AddDays(10);
        notCovering.EndDate = today.AddDays(20);

        ctx.EmployeeScheduleTemplates.AddRange(covering, notCovering);
        await ctx.SaveChangesAsync();

        var repo = new ScheduleRepository(ctx);
        var result = (await repo.GetActiveTemplatesForDateAsync(today)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_Found_ReturnsWithEmployee()
    {
        var (ctx, emp) = await SetupAsync();
        ctx.EmployeeScheduleTemplates.Add(Seed.Template(1, emp.Id));
        await ctx.SaveChangesAsync();

        var repo = new ScheduleRepository(ctx);
        var tmpl = await repo.GetTemplateByIdAsync(1);

        Assert.NotNull(tmpl);
        Assert.NotNull(tmpl!.Employee);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_NotFound_ReturnsNull()
    {
        var (ctx, _) = await SetupAsync();
        var repo = new ScheduleRepository(ctx);
        Assert.Null(await repo.GetTemplateByIdAsync(999));
    }

    [Fact]
    public async Task AddTemplate_ThenRemove_LeavesNoTemplate()
    {
        var (ctx, emp) = await SetupAsync();
        var tmpl = Seed.Template(1, emp.Id);
        var repo = new ScheduleRepository(ctx);

        await repo.AddTemplateAsync(tmpl);
        await ctx.SaveChangesAsync();
        Assert.NotNull(await repo.GetTemplateByIdAsync(1));

        repo.RemoveTemplate(tmpl);
        await ctx.SaveChangesAsync();
        Assert.Null(await repo.GetTemplateByIdAsync(1));
    }

    [Fact]
    public async Task UpdateTemplate_PersistsChanges()
    {
        var (ctx, emp) = await SetupAsync();
        var tmpl = Seed.Template(1, emp.Id);
        ctx.EmployeeScheduleTemplates.Add(tmpl);
        await ctx.SaveChangesAsync();

        tmpl.WorkingDays = 2; // Monday only
        var repo = new ScheduleRepository(ctx);
        repo.UpdateTemplate(tmpl);
        await ctx.SaveChangesAsync();

        var updated = await repo.GetTemplateByIdAsync(1);
        Assert.Equal(2, updated!.WorkingDays);
    }

    // ── Leaves ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsOnLeaveAsync_EmployeeOnLeave_ReturnsTrue()
    {
        var (ctx, emp) = await SetupAsync();
        var today = DateTime.UtcNow.Date;
        ctx.EmployeeLeaves.Add(Seed.Leave(1, emp.Id, today));
        await ctx.SaveChangesAsync();

        var repo = new ScheduleRepository(ctx);
        Assert.True(await repo.IsOnLeaveAsync(emp.Id, today));
    }

    [Fact]
    public async Task IsOnLeaveAsync_NotOnLeave_ReturnsFalse()
    {
        var (ctx, emp) = await SetupAsync();
        var repo = new ScheduleRepository(ctx);
        Assert.False(await repo.IsOnLeaveAsync(emp.Id, DateTime.UtcNow.Date));
    }

    [Fact]
    public async Task IsOnLeaveAsync_DifferentDate_ReturnsFalse()
    {
        var (ctx, emp) = await SetupAsync();
        var today = DateTime.UtcNow.Date;
        ctx.EmployeeLeaves.Add(Seed.Leave(1, emp.Id, today));
        await ctx.SaveChangesAsync();

        var repo = new ScheduleRepository(ctx);
        Assert.False(await repo.IsOnLeaveAsync(emp.Id, today.AddDays(1)));
    }

    [Fact]
    public async Task GetLeavesByEmployeeAsync_OnlyForEmployee()
    {
        var (ctx, emp) = await SetupAsync();
        var emp2 = Seed.Employee(2, "Dave", "Brown");
        ctx.Users.Add(emp2);
        ctx.EmployeeLeaves.AddRange(
            Seed.Leave(1, emp.Id),
            Seed.Leave(2, emp2.Id));
        await ctx.SaveChangesAsync();

        var repo = new ScheduleRepository(ctx);
        var result = (await repo.GetLeavesByEmployeeAsync(emp.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(emp.Id, result[0].EmployeeId);
    }

    [Fact]
    public async Task GetAllLeavesAsync_ReturnsAllLeaves()
    {
        var (ctx, emp) = await SetupAsync();
        var emp2 = Seed.Employee(2, "Dave", "Brown");
        ctx.Users.Add(emp2);
        ctx.EmployeeLeaves.AddRange(
            Seed.Leave(1, emp.Id),
            Seed.Leave(2, emp2.Id));
        await ctx.SaveChangesAsync();

        var repo = new ScheduleRepository(ctx);
        Assert.Equal(2, (await repo.GetAllLeavesAsync()).Count());
    }

    [Fact]
    public async Task GetLeaveByIdAsync_Found_ReturnsWithEmployee()
    {
        var (ctx, emp) = await SetupAsync();
        ctx.EmployeeLeaves.Add(Seed.Leave(1, emp.Id));
        await ctx.SaveChangesAsync();

        var repo = new ScheduleRepository(ctx);
        var leave = await repo.GetLeaveByIdAsync(1);

        Assert.NotNull(leave);
        Assert.NotNull(leave!.Employee);
    }

    [Fact]
    public async Task AddLeave_ThenRemove_LeavesNoLeave()
    {
        var (ctx, emp) = await SetupAsync();
        var leave = Seed.Leave(1, emp.Id);
        var repo = new ScheduleRepository(ctx);

        await repo.AddLeaveAsync(leave);
        await ctx.SaveChangesAsync();
        Assert.NotNull(await repo.GetLeaveByIdAsync(1));

        repo.RemoveLeave(leave);
        await ctx.SaveChangesAsync();
        Assert.Null(await repo.GetLeaveByIdAsync(1));
    }
}
