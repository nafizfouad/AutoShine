using MapsterMapper;
using AutoShine.Models.Entities;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.DTOs.Common;
using AutoShine.Service.DTOs.Inventory;
using AutoShine.Service.Interfaces;

namespace AutoShine.Service.Implementations;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public InventoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<InventoryItemDto>> GetInventoryAsync(int page, int pageSize, bool? lowStockOnly = null)
    {
        var (items, total) = await _unitOfWork.Inventory.GetPagedAsync(
            page, pageSize,
            filter: i => lowStockOnly != true || i.CurrentStock <= i.MinimumThreshold,
            orderBy: q => q.OrderBy(i => i.ItemName));

        return new PagedResult<InventoryItemDto>
        {
            Items = _mapper.Map<IEnumerable<InventoryItemDto>>(items),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<InventoryItemDto?> GetItemByIdAsync(int id)
    {
        var item = await _unitOfWork.Inventory.GetByIdAsync(id);
        return item == null ? null : _mapper.Map<InventoryItemDto>(item);
    }

    public async Task<InventoryItemDto> CreateItemAsync(CreateInventoryItemDto dto)
    {
        var existing = await _unitOfWork.Inventory.GetBySkuAsync(dto.SKU);
        if (existing != null)
            throw new InvalidOperationException($"SKU '{dto.SKU}' already exists.");

        var item = new InventoryItem
        {
            ItemName = dto.ItemName,
            SKU = dto.SKU,
            CurrentStock = dto.CurrentStock,
            MinimumThreshold = dto.MinimumThreshold,
            Unit = dto.Unit
        };

        await _unitOfWork.Inventory.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<InventoryItemDto>(item);
    }

    public async Task<InventoryItemDto?> UpdateItemAsync(int id, UpdateInventoryItemDto dto)
    {
        var item = await _unitOfWork.Inventory.GetByIdAsync(id);
        if (item == null) return null;

        item.ItemName = dto.ItemName;
        item.CurrentStock = dto.CurrentStock;
        item.MinimumThreshold = dto.MinimumThreshold;
        item.Unit = dto.Unit;

        _unitOfWork.Inventory.Update(item);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<InventoryItemDto>(item);
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        var item = await _unitOfWork.Inventory.GetByIdAsync(id);
        if (item == null) return false;

        _unitOfWork.Inventory.Remove(item);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetLowStockAlertsAsync()
    {
        var items = await _unitOfWork.Inventory.GetLowStockItemsAsync();
        return _mapper.Map<IEnumerable<InventoryItemDto>>(items);
    }
}
