using AutoShine.Service.DTOs.Common;
using AutoShine.Service.DTOs.Users;

namespace AutoShine.Service.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? role = null, string? search = null);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto dto);
    Task<bool> DeleteUserAsync(int id);
}
