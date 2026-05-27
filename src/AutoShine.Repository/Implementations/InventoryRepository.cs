using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoShine.Repository.Implementations;

public class InventoryRepository : GenericRepository<InventoryItem>, IInventoryRepository
{
    public InventoryRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync()
        => await _dbSet.Where(i => i.CurrentStock <= i.MinimumThreshold).ToListAsync();

    public async Task<InventoryItem?> GetBySkuAsync(string sku)
        => await _dbSet.FirstOrDefaultAsync(i => i.SKU == sku);

    public async Task<bool> DeductStockAsync(int itemId, int quantity)
    {
        var item = await _dbSet.FindAsync(itemId);
        if (item == null || item.CurrentStock < quantity)
            return false;

        item.CurrentStock -= quantity;
        return true;
    }
}
