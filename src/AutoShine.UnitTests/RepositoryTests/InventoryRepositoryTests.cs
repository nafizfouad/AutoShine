using AutoShine.Repository.Implementations;
using AutoShine.UnitTests.Helpers;
using Xunit;

namespace AutoShine.UnitTests.RepositoryTests;

public class InventoryRepositoryTests
{
    // ── GetLowStockItemsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetLowStockItemsAsync_ReturnsItemsAtOrBelowThreshold()
    {
        using var ctx = DbContextFactory.Create();
        var low = Seed.InventoryItem(1, stock: 3);
        low.MinimumThreshold = 5;
        var ok = Seed.InventoryItem(2, stock: 50);
        ok.MinimumThreshold = 5;
        ctx.InventoryItems.AddRange(low, ok);
        await ctx.SaveChangesAsync();

        var repo = new InventoryRepository(ctx);
        var result = (await repo.GetLowStockItemsAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetLowStockItemsAsync_ItemAtExactThreshold_IsIncluded()
    {
        using var ctx = DbContextFactory.Create();
        var item = Seed.InventoryItem(1, stock: 5);
        item.MinimumThreshold = 5;
        ctx.InventoryItems.Add(item);
        await ctx.SaveChangesAsync();

        var repo = new InventoryRepository(ctx);
        var result = await repo.GetLowStockItemsAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetLowStockItemsAsync_NoLowStock_ReturnsEmpty()
    {
        using var ctx = DbContextFactory.Create();
        var item = Seed.InventoryItem(1, stock: 100);
        ctx.InventoryItems.Add(item);
        await ctx.SaveChangesAsync();

        var repo = new InventoryRepository(ctx);
        Assert.Empty(await repo.GetLowStockItemsAsync());
    }

    // ── GetBySkuAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBySkuAsync_ExistingSku_ReturnsItem()
    {
        using var ctx = DbContextFactory.Create();
        ctx.InventoryItems.Add(Seed.InventoryItem(1));
        await ctx.SaveChangesAsync();

        var repo = new InventoryRepository(ctx);
        var item = await repo.GetBySkuAsync("SKU-001");

        Assert.NotNull(item);
        Assert.Equal(1, item!.Id);
    }

    [Fact]
    public async Task GetBySkuAsync_NonExistentSku_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create();
        var repo = new InventoryRepository(ctx);
        Assert.Null(await repo.GetBySkuAsync("NOPE"));
    }

    // ── DeductStockAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeductStockAsync_SufficientStock_ReturnsTrueAndDeducts()
    {
        using var ctx = DbContextFactory.Create();
        ctx.InventoryItems.Add(Seed.InventoryItem(1, stock: 20));
        await ctx.SaveChangesAsync();

        var repo = new InventoryRepository(ctx);
        var success = await repo.DeductStockAsync(1, 5);
        await ctx.SaveChangesAsync();

        Assert.True(success);
        var item = await repo.GetByIdAsync(1);
        Assert.Equal(15, item!.CurrentStock);
    }

    [Fact]
    public async Task DeductStockAsync_InsufficientStock_ReturnsFalse()
    {
        using var ctx = DbContextFactory.Create();
        ctx.InventoryItems.Add(Seed.InventoryItem(1, stock: 2));
        await ctx.SaveChangesAsync();

        var repo = new InventoryRepository(ctx);
        var success = await repo.DeductStockAsync(1, 10);

        Assert.False(success);
        // Stock must NOT be modified
        var item = await repo.GetByIdAsync(1);
        Assert.Equal(2, item!.CurrentStock);
    }

    [Fact]
    public async Task DeductStockAsync_ExactStock_ReturnsTrueAndDeductsToZero()
    {
        using var ctx = DbContextFactory.Create();
        ctx.InventoryItems.Add(Seed.InventoryItem(1, stock: 5));
        await ctx.SaveChangesAsync();

        var repo = new InventoryRepository(ctx);
        var success = await repo.DeductStockAsync(1, 5);
        await ctx.SaveChangesAsync();

        Assert.True(success);
        var item = await repo.GetByIdAsync(1);
        Assert.Equal(0, item!.CurrentStock);
    }

    [Fact]
    public async Task DeductStockAsync_NonExistentItem_ReturnsFalse()
    {
        using var ctx = DbContextFactory.Create();
        var repo = new InventoryRepository(ctx);
        Assert.False(await repo.DeductStockAsync(999, 1));
    }
}
