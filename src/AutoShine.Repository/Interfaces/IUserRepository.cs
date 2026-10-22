using AutoShine.Models.Entities;
using AutoShine.Models.Enums;

namespace AutoShine.Repository.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
}
