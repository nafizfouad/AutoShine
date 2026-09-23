using AutoShine.Service.DTOs.Inventory;
using AutoShine.Service.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.ServiceTests;

public class InventoryServiceTests
{
    // ── GetInventoryAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetInventoryAsync_ReturnsAllItems()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.InventoryItem(1, 50), Seed.InventoryItem(2, 10));

        var svc = new InventoryService(fx.Uow, fx.Mapper);
        var result = await svc.GetInventoryAsync(1, 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetInventoryAsync_LowStockFilter_OnlyReturnsLowItems()
    {
        using var fx = new ServiceFixture();
        var low = Seed.InventoryItem(1, stock: 2); low.MinimumThreshold = 10;
        var ok  = Seed.InventoryItem(2, stock: 50); ok.MinimumThreshold = 10;
        await fx.SeedAsync(low, ok);

        var svc = new InventoryService(fx.Uow, fx.Mapper);
        var result = await svc.GetInventoryAsync(1, 10, lowStockOnly: true);

        Assert.Equal(1, result.TotalCount);
        Assert.True(result.Items.All(i => i.IsLowStock));
    }

    [Fact]
    public async Task GetInventoryAsync_IsLowStock_TrueWhenAtThreshold()
    {
        using var fx = new ServiceFixture();
        var item = Seed.InventoryItem(1, stock: 5); item.MinimumThreshold = 5;
        await fx.SeedAsync(item);

        var svc = new InventoryService(fx.Uow, fx.Mapper);
        var result = await svc.GetInventoryAsync(1, 10);

        Assert.True(result.Items.First().IsLowStock);
    }

    // ── GetItemByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetItemByIdAsync_Found_ReturnsDto()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.InventoryItem(1));

        var svc = new InventoryService(fx.Uow, fx.Mapper);
        var result = await svc.GetItemByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Item 1", result!.ItemName);
    }

    [Fact]
    public async Task GetItemByIdAsync_NotFound_ReturnsNull()
    {
        using var fx = new ServiceFixture();
        var svc = new InventoryService(fx.Uow, fx.Mapper);
        Assert.Null(await svc.GetItemByIdAsync(999));
    }

    // ── CreateItemAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateItemAsync_NewSku_CreatesAndReturns()
    {
        using var fx = new ServiceFixture();
        var svc = new InventoryService(fx.Uow, fx.Mapper);
        var dto = new CreateInventoryItemDto("Wax", "WAX-001", 100, 10, "liters");

        var result = await svc.CreateItemAsync(dto);

        Assert.Equal("Wax", result.ItemName);
        Assert.Equal("WAX-001", result.SKU);
        Assert.False(result.IsLowStock); // 100 > 10
    }

    [Fact]
    public async Task CreateItemAsync_DuplicateSku_Throws()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.InventoryItem(1)); // has SKU-001
        var svc = new InventoryService(fx.Uow, fx.Mapper);
        var dto = new CreateInventoryItemDto("Other", "SKU-001", 5, 1, "pieces");

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateItemAsync(dto));
    }

    // ── UpdateItemAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_ExistingItem_UpdatesFields()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.InventoryItem(1, stock: 50));
        var svc = new InventoryService(fx.Uow, fx.Mapper);

        var dto = new UpdateInventoryItemDto("Renamed Wax", 200, 20, "liters");
        var result = await svc.UpdateItemAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal("Renamed Wax", result!.ItemName);
        Assert.Equal(200, result.CurrentStock);
    }

    [Fact]
    public async Task UpdateItemAsync_NotFound_ReturnsNull()
    {
        using var fx = new ServiceFixture();
        var svc = new InventoryService(fx.Uow, fx.Mapper);
        Assert.Null(await svc.UpdateItemAsync(999, new UpdateInventoryItemDto("X", 0, 0, "pieces")));
    }

    // ── DeleteItemAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteItemAsync_ExistingItem_DeletesAndReturnsTrue()
    {
        using var fx = new ServiceFixture();
        await fx.SeedAsync(Seed.InventoryItem(1));
        var svc = new InventoryService(fx.Uow, fx.Mapper);

        Assert.True(await svc.DeleteItemAsync(1));
        Assert.Null(await svc.GetItemByIdAsync(1));
    }

    [Fact]
    public async Task DeleteItemAsync_NotFound_ReturnsFalse()
    {
        using var fx = new ServiceFixture();
        var svc = new InventoryService(fx.Uow, fx.Mapper);
        Assert.False(await svc.DeleteItemAsync(999));
    }

    // ── GetLowStockAlertsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetLowStockAlertsAsync_OnlyLowItems()
    {
        using var fx = new ServiceFixture();
        var low  = Seed.InventoryItem(1, stock: 1); low.MinimumThreshold = 10;
        var fine = Seed.InventoryItem(2, stock: 100); fine.MinimumThreshold = 10;
        await fx.SeedAsync(low, fine);

        var svc = new InventoryService(fx.Uow, fx.Mapper);
        var alerts = (await svc.GetLowStockAlertsAsync()).ToList();

        Assert.Single(alerts);
        Assert.Equal("Item 1", alerts[0].ItemName);
    }
}
