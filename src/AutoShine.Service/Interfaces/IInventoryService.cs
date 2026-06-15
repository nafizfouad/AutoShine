using AutoShine.Service.DTOs.Common;
using AutoShine.Service.DTOs.Inventory;

namespace AutoShine.Service.Interfaces;

public interface IInventoryService
{
    Task<PagedResult<InventoryItemDto>> GetInventoryAsync(int page, int pageSize, bool? lowStockOnly = null);
    Task<InventoryItemDto?> GetItemByIdAsync(int id);
    Task<InventoryItemDto> CreateItemAsync(CreateInventoryItemDto dto);
    Task<InventoryItemDto?> UpdateItemAsync(int id, UpdateInventoryItemDto dto);
    Task<bool> DeleteItemAsync(int id);
    Task<IEnumerable<InventoryItemDto>> GetLowStockAlertsAsync();
}
