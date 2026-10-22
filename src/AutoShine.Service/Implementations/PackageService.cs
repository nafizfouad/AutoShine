using MapsterMapper;
using AutoShine.Models.Entities;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.DTOs.Packages;
using AutoShine.Service.Interfaces;

namespace AutoShine.Service.Implementations;

public class PackageService : IPackageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PackageService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PackageDto>> GetAllPackagesAsync(bool activeOnly = true)
    {
        // Both branches load the recipe items: the admin edit form relies on them,
        // and saving a package without them would wipe its recipe.
        var packages = activeOnly
            ? await _unitOfWork.Packages.GetActivePackagesAsync()
            : await _unitOfWork.Packages.GetAllPackagesWithItemsAsync();

        return _mapper.Map<IEnumerable<PackageDto>>(packages);
    }

    public async Task<PackageDto?> GetPackageByIdAsync(int id)
    {
        var pkg = await _unitOfWork.Packages.GetPackageWithItemsAsync(id);
        return pkg == null ? null : _mapper.Map<PackageDto>(pkg);
    }

    public async Task<PackageDto> CreatePackageAsync(CreatePackageDto dto)
    {
        ValidatePackage(dto.Name, dto.Description, dto.Price, dto.EstimatedDurationMinutes);
        await ValidateItemsAsync(dto.Items);

        var package = new Package
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            Price = dto.Price,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            IsActive = dto.IsActive,
            PackageItems = (dto.Items ?? new()).Select(i => new PackageItem
            {
                InventoryItemId = i.InventoryItemId,
                QuantityRequired = i.QuantityRequired
            }).ToList()
        };

        await _unitOfWork.Packages.AddAsync(package);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<PackageDto>((await _unitOfWork.Packages.GetPackageWithItemsAsync(package.Id))!);
    }

    public async Task<PackageDto?> UpdatePackageAsync(int id, UpdatePackageDto dto)
    {
        var pkg = await _unitOfWork.Packages.GetPackageWithItemsAsync(id);
        if (pkg == null) return null;

        ValidatePackage(dto.Name, dto.Description, dto.Price, dto.EstimatedDurationMinutes);
        await ValidateItemsAsync(dto.Items);

        pkg.Name = dto.Name.Trim();
        pkg.Description = dto.Description?.Trim() ?? string.Empty;
        pkg.Price = dto.Price;
        pkg.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;
        pkg.IsActive = dto.IsActive;

        // Replace recipe items
        pkg.PackageItems.Clear();
        foreach (var item in dto.Items ?? new())
        {
            pkg.PackageItems.Add(new PackageItem
            {
                PackageId = id,
                InventoryItemId = item.InventoryItemId,
                QuantityRequired = item.QuantityRequired
            });
        }

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<PackageDto>((await _unitOfWork.Packages.GetPackageWithItemsAsync(id))!);
    }

    public async Task<bool> DeletePackageAsync(int id)
    {
        var pkg = await _unitOfWork.Packages.GetByIdAsync(id);
        if (pkg == null) return false;

        // Soft delete: bookings keep referencing the package.
        pkg.IsActive = false;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static void ValidatePackage(string? name, string? description, decimal price, int durationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            throw new ArgumentException("Package name is required and must be at most 200 characters.");
        if (description?.Trim().Length > 1000)
            throw new ArgumentException("Description must be at most 1000 characters.");
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");
        if (durationMinutes <= 0 || durationMinutes > 24 * 60)
            throw new ArgumentException("Duration must be between 1 and 1440 minutes.");
    }

    private async Task ValidateItemsAsync(List<CreatePackageItemDto>? items)
    {
        if (items == null || items.Count == 0) return;

        if (items.Any(i => i.QuantityRequired <= 0))
            throw new ArgumentException("Each item quantity must be at least 1.");
        if (items.Select(i => i.InventoryItemId).Distinct().Count() != items.Count)
            throw new ArgumentException("Each inventory item can only appear once in a package.");

        foreach (var itemId in items.Select(i => i.InventoryItemId))
        {
            if (!await _unitOfWork.Inventory.AnyAsync(i => i.Id == itemId))
                throw new ArgumentException($"Inventory item {itemId} does not exist.");
        }
    }
}
