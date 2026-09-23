using AutoShine.Service.DTOs.Packages;
using AutoShine.Service.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.ServiceTests;

public class PackageServiceTests
{
    // ── GetAllPackagesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPackagesAsync_ActiveOnly_ReturnsOnlyActive()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Package(1, active: true), Seed.Package(2, active: false));

        var svc = new PackageService(fx.Uow, fx.Mapper);
        var result = (await svc.GetAllPackagesAsync(activeOnly: true)).ToList();

        Assert.Single(result);
        Assert.True(result[0].IsActive);
    }

    [Fact]
    public async Task GetAllPackagesAsync_AllPackages_ReturnsAll()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Package(1, active: true), Seed.Package(2, active: false));

        var svc = new PackageService(fx.Uow, fx.Mapper);
        var result = (await svc.GetAllPackagesAsync(activeOnly: false)).ToList();

        Assert.Equal(2, result.Count);
    }

    // ── GetPackageByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPackageByIdAsync_Found_ReturnsDtoWithItems()
    {
        using var fx = new ServiceFixture();
        var invItem = Seed.InventoryItem(1);
        var pkg = Seed.Package(1);
        await fx.SeedAsync(invItem, pkg);
        fx.Ctx.PackageItems.Add(new AutoShine.Models.Entities.PackageItem
        {
            Id = 1, PackageId = 1, InventoryItemId = 1, QuantityRequired = 2
        });
        await fx.Ctx.SaveChangesAsync();

        var svc = new PackageService(fx.Uow, fx.Mapper);
        var result = await svc.GetPackageByIdAsync(1);

        Assert.NotNull(result);
        Assert.Single(result!.Items);
    }

    [Fact]
    public async Task GetPackageByIdAsync_NotFound_ReturnsNull()
    {
        using var fx = new ServiceFixture();
        var svc = new PackageService(fx.Uow, fx.Mapper);
        Assert.Null(await svc.GetPackageByIdAsync(999));
    }

    // ── CreatePackageAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePackageAsync_NoItems_CreatesPackage()
    {
        using var fx = new ServiceFixture();
        var svc = new PackageService(fx.Uow, fx.Mapper);
        var dto = new CreatePackageDto("Basic Wash", "Desc", 29.99m, 30, new List<CreatePackageItemDto>());

        var result = await svc.CreatePackageAsync(dto);

        Assert.Equal("Basic Wash", result.Name);
        Assert.Equal(29.99m, result.Price);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task CreatePackageAsync_WithItems_PersistsItems()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.InventoryItem(1));
        var svc = new PackageService(fx.Uow, fx.Mapper);

        var dto = new CreatePackageDto("Full Detail", "Desc", 99m, 120,
            new List<CreatePackageItemDto> { new(InventoryItemId: 1, QuantityRequired: 3) });

        var result = await svc.CreatePackageAsync(dto);

        Assert.Single(result.Items);
        Assert.Equal(3, result.Items[0].QuantityRequired);
    }

    // ── UpdatePackageAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePackageAsync_ExistingPackage_UpdatesFields()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Package(1));
        var svc = new PackageService(fx.Uow, fx.Mapper);

        var dto = new UpdatePackageDto("Renamed", "New desc", 59m, 45, false,
            new List<CreatePackageItemDto>());

        var result = await svc.UpdatePackageAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal("Renamed", result!.Name);
        Assert.Equal(59m, result.Price);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdatePackageAsync_NotFound_ReturnsNull()
    {
        using var fx = new ServiceFixture();
        var svc = new PackageService(fx.Uow, fx.Mapper);
        var dto = new UpdatePackageDto("X", "Y", 10m, 30, true, new List<CreatePackageItemDto>());
        Assert.Null(await svc.UpdatePackageAsync(999, dto));
    }

    [Fact]
    public async Task UpdatePackageAsync_ReplacesItems()
    {
        using var fx = new ServiceFixture();
        var inv1 = Seed.InventoryItem(1); var inv2 = Seed.InventoryItem(2);
        var pkg = Seed.Package(1);
        await fx.SeedAsync(inv1, inv2, pkg);
        fx.Ctx.PackageItems.Add(new AutoShine.Models.Entities.PackageItem
        {
            Id = 1, PackageId = 1, InventoryItemId = 1, QuantityRequired = 1
        });
        await fx.Ctx.SaveChangesAsync();

        var svc = new PackageService(fx.Uow, fx.Mapper);
        var dto = new UpdatePackageDto("Same", "Desc", 49m, 60, true,
            new List<CreatePackageItemDto> { new(InventoryItemId: 2, QuantityRequired: 5) });

        var result = await svc.UpdatePackageAsync(1, dto);

        Assert.Single(result!.Items);
        Assert.Equal(2, result.Items[0].InventoryItemId);
    }

    // ── DeletePackageAsync (soft delete) ─────────────────────────────────────

    [Fact]
    public async Task DeletePackageAsync_ExistingPackage_SetsIsActiveFalse()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.Package(1));
        var svc = new PackageService(fx.Uow, fx.Mapper);

        Assert.True(await svc.DeletePackageAsync(1));
        var pkg = await fx.Uow.Packages.GetByIdAsync(1);
        Assert.False(pkg!.IsActive);
    }

    [Fact]
    public async Task DeletePackageAsync_NotFound_ReturnsFalse()
    {
        using var fx = new ServiceFixture();
        var svc = new PackageService(fx.Uow, fx.Mapper);
        Assert.False(await svc.DeletePackageAsync(999));
    }
}
