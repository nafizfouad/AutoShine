using MapsterMapper;
using AutoShine.Models.Entities;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.Common;
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
        (page, pageSize) = Paging.Normalize(page, pageSize);

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
        var sku = dto.SKU?.Trim() ?? string.Empty;
        if (sku.Length == 0 || sku.Length > 50)
            throw new ArgumentException("SKU is required and must be at most 50 characters.");
        ValidateItem(dto.ItemName, dto.CurrentStock, dto.MinimumThreshold, dto.Unit);

        var existing = await _unitOfWork.Inventory.GetBySkuAsync(sku);
        if (existing != null)
            throw new InvalidOperationException($"SKU '{sku}' already exists.");

        var item = new InventoryItem
        {
            ItemName = dto.ItemName.Trim(),
            SKU = sku,
            CurrentStock = dto.CurrentStock,
            MinimumThreshold = dto.MinimumThreshold,
            Unit = dto.Unit.Trim()
        };

        await _unitOfWork.Inventory.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<InventoryItemDto>(item);
    }

    public async Task<InventoryItemDto?> UpdateItemAsync(int id, UpdateInventoryItemDto dto)
    {
        var item = await _unitOfWork.Inventory.GetByIdAsync(id);
        if (item == null) return null;

        ValidateItem(dto.ItemName, dto.CurrentStock, dto.MinimumThreshold, dto.Unit);

        item.ItemName = dto.ItemName.Trim();
        item.CurrentStock = dto.CurrentStock;
        item.MinimumThreshold = dto.MinimumThreshold;
        item.Unit = dto.Unit.Trim();

        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<InventoryItemDto>(item);
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        var item = await _unitOfWork.Inventory.GetByIdAsync(id);
        if (item == null) return false;

        var usedBy = await _unitOfWork.Inventory.GetPackageNamesUsingItemAsync(id);
        if (usedBy.Count > 0)
            throw new InvalidOperationException(
                $"'{item.ItemName}' is used by: {string.Join(", ", usedBy)}. Remove it from those packages first.");

        _unitOfWork.Inventory.Remove(item);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetLowStockAlertsAsync()
    {
        var items = await _unitOfWork.Inventory.GetLowStockItemsAsync();
        return _mapper.Map<IEnumerable<InventoryItemDto>>(items);
    }

    private static void ValidateItem(string? itemName, int currentStock, int minimumThreshold, string? unit)
    {
        if (string.IsNullOrWhiteSpace(itemName) || itemName.Trim().Length > 200)
            throw new ArgumentException("Item name is required and must be at most 200 characters.");
        if (currentStock < 0)
            throw new ArgumentException("Current stock cannot be negative.");
        if (minimumThreshold < 0)
            throw new ArgumentException("Minimum threshold cannot be negative.");
        if (string.IsNullOrWhiteSpace(unit) || unit.Trim().Length > 50)
            throw new ArgumentException("Unit is required and must be at most 50 characters.");
    }
}
