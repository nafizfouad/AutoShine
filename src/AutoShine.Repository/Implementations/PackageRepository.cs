using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoShine.Repository.Implementations;

public class PackageRepository : GenericRepository<Package>, IPackageRepository
{
    public PackageRepository(AppDbContext context) : base(context) { }

    public async Task<Package?> GetPackageWithItemsAsync(int packageId)
        => await _dbSet
            .Include(p => p.PackageItems)
                .ThenInclude(pi => pi.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == packageId);

    public async Task<IEnumerable<Package>> GetActivePackagesAsync()
        => await _dbSet
            .Where(p => p.IsActive)
            .Include(p => p.PackageItems)
                .ThenInclude(pi => pi.InventoryItem)
            .OrderBy(p => p.Price)
            .ToListAsync();
}
