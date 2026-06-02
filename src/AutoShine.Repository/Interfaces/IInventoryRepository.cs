using AutoShine.Models.Entities;

namespace AutoShine.Repository.Interfaces;

public interface IInventoryRepository : IGenericRepository<InventoryItem>
{
    Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync();
    Task<InventoryItem?> GetBySkuAsync(string sku);
    Task<bool> DeductStockAsync(int itemId, int quantity);
}
