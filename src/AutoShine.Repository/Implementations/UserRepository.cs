using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoShine.Repository.Implementations;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLower();
        return await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role)
        => await _dbSet.Where(u => u.Role == role && u.IsActive).ToListAsync();
}
