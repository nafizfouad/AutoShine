using AutoShine.Repository.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.RepositoryTests;

public class PackageRepositoryTests
{
    [Fact]
    public async Task GetPackageWithItemsAsync_ReturnsPackageWithItems()
    {
        using var ctx = DbContextFactory.Create();
        var invItem = Seed.InventoryItem(1);
        var pkg = Seed.Package(1);
        ctx.InventoryItems.Add(invItem);
        ctx.Packages.Add(pkg);
        ctx.PackageItems.Add(new AutoShine.Models.Entities.PackageItem
        {
            Id = 1, PackageId = pkg.Id, InventoryItemId = invItem.Id, QuantityRequired = 2
        });
        await ctx.SaveChangesAsync();

        var repo = new PackageRepository(ctx);
        var result = await repo.GetPackageWithItemsAsync(1);

        Assert.NotNull(result);
        Assert.Single(result!.PackageItems);
        Assert.NotNull(result.PackageItems.First().InventoryItem);
    }

    [Fact]
    public async Task GetPackageWithItemsAsync_NotFound_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create();
        var repo = new PackageRepository(ctx);
        Assert.Null(await repo.GetPackageWithItemsAsync(999));
    }

    [Fact]
    public async Task GetPackageWithItemsAsync_PackageWithNoItems_ReturnsEmptyItems()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Packages.Add(Seed.Package(1));
        await ctx.SaveChangesAsync();

        var repo = new PackageRepository(ctx);
        var result = await repo.GetPackageWithItemsAsync(1);

        Assert.NotNull(result);
        Assert.Empty(result!.PackageItems);
    }

    [Fact]
    public async Task GetActivePackagesAsync_ReturnsOnlyActivePackages()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Packages.AddRange(
            Seed.Package(1, active: true),
            Seed.Package(2, active: false),
            Seed.Package(3, active: true));
        await ctx.SaveChangesAsync();

        var repo = new PackageRepository(ctx);
        var result = (await repo.GetActivePackagesAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task GetActivePackagesAsync_OrdersByPrice()
    {
        using var ctx = DbContextFactory.Create();
        var cheap = Seed.Package(1); cheap.Price = 10m;
        var expensive = Seed.Package(2); expensive.Price = 100m;
        ctx.Packages.AddRange(cheap, expensive);
        await ctx.SaveChangesAsync();

        var repo = new PackageRepository(ctx);
        var result = (await repo.GetActivePackagesAsync()).ToList();

        Assert.Equal(1, result[0].Id); // cheapest first
    }

    [Fact]
    public async Task GetActivePackagesAsync_NoActivePackages_ReturnsEmpty()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Packages.Add(Seed.Package(1, active: false));
        await ctx.SaveChangesAsync();

        var repo = new PackageRepository(ctx);
        Assert.Empty(await repo.GetActivePackagesAsync());
    }
}
