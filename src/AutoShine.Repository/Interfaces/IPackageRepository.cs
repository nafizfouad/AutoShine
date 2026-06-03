using AutoShine.Models.Entities;

namespace AutoShine.Repository.Interfaces;

public interface IPackageRepository : IGenericRepository<Package>
{
    Task<Package?> GetPackageWithItemsAsync(int packageId);
    Task<IEnumerable<Package>> GetActivePackagesAsync();
}
