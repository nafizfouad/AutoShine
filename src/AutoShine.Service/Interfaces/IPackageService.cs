using AutoShine.Service.DTOs.Packages;

namespace AutoShine.Service.Interfaces;

public interface IPackageService
{
    Task<IEnumerable<PackageDto>> GetAllPackagesAsync(bool activeOnly = true);
    Task<PackageDto?> GetPackageByIdAsync(int id);
    Task<PackageDto> CreatePackageAsync(CreatePackageDto dto);
    Task<PackageDto?> UpdatePackageAsync(int id, UpdatePackageDto dto);
    Task<bool> DeletePackageAsync(int id);
}
