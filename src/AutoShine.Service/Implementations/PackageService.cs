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
        var packages = activeOnly
            ? await _unitOfWork.Packages.GetActivePackagesAsync()
            : await _unitOfWork.Packages.GetAllAsync();

        return _mapper.Map<IEnumerable<PackageDto>>(packages);
    }

    public async Task<PackageDto?> GetPackageByIdAsync(int id)
    {
        var pkg = await _unitOfWork.Packages.GetPackageWithItemsAsync(id);
        return pkg == null ? null : _mapper.Map<PackageDto>(pkg);
    }

    public async Task<PackageDto> CreatePackageAsync(CreatePackageDto dto)
    {
        var package = new Package
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            PackageItems = dto.Items.Select(i => new PackageItem
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

        pkg.Name = dto.Name;
        pkg.Description = dto.Description;
        pkg.Price = dto.Price;
        pkg.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;
        pkg.IsActive = dto.IsActive;

        // Replace recipe items
        pkg.PackageItems.Clear();
        foreach (var item in dto.Items)
        {
            pkg.PackageItems.Add(new PackageItem
            {
                PackageId = id,
                InventoryItemId = item.InventoryItemId,
                QuantityRequired = item.QuantityRequired
            });
        }

        _unitOfWork.Packages.Update(pkg);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<PackageDto>((await _unitOfWork.Packages.GetPackageWithItemsAsync(id))!);
    }

    public async Task<bool> DeletePackageAsync(int id)
    {
        var pkg = await _unitOfWork.Packages.GetByIdAsync(id);
        if (pkg == null) return false;

        // Soft delete
        pkg.IsActive = false;
        _unitOfWork.Packages.Update(pkg);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
