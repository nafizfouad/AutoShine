using MapsterMapper;
using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.Common;
using AutoShine.Service.DTOs.Common;
using AutoShine.Service.DTOs.Users;
using AutoShine.Service.Interfaces;

namespace AutoShine.Service.Implementations;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? role = null, string? search = null)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        UserRole? roleFilter = role != null && Enum.TryParse<UserRole>(role, true, out var r) ? r : null;
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        // ToLower() on both sides keeps the search case-insensitive on PostgreSQL.
        var (items, total) = await _unitOfWork.Users.GetPagedAsync(
            page, pageSize,
            filter: u =>
                (roleFilter == null || u.Role == roleFilter) &&
                (term == null
                    || u.FirstName.ToLower().Contains(term)
                    || u.LastName.ToLower().Contains(term)
                    || u.Email.ToLower().Contains(term)),
            orderBy: q => q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName));

        return new PagedResult<UserDto>
        {
            Items = _mapper.Map<IEnumerable<UserDto>>(items),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        var firstName = Validate.Name(dto.FirstName, "First name");
        var lastName = Validate.Name(dto.LastName, "Last name");
        var email = Validate.Email(dto.Email);
        var phone = Validate.Phone(dto.Phone, required: true);
        Validate.Password(dto.Password);
        EnsureValidRole(dto.Role);

        var existing = await _unitOfWork.Users.GetByEmailAsync(email);
        if (existing != null)
            throw new InvalidOperationException("Email already in use.");

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = phone,
            Role = dto.Role
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return null;

        var email = Validate.Email(dto.Email);
        EnsureValidRole(dto.Role);
        if (email != user.Email)
        {
            var existing = await _unitOfWork.Users.GetByEmailAsync(email);
            if (existing != null && existing.Id != id)
                throw new InvalidOperationException("Email already in use.");
        }

        user.FirstName = Validate.Name(dto.FirstName, "First name");
        user.LastName = Validate.Name(dto.LastName, "Last name");
        user.Email = email;
        user.Phone = Validate.Phone(dto.Phone, required: false);
        user.Role = dto.Role;
        user.IsActive = dto.IsActive;

        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return false;

        // Soft delete
        user.IsActive = false;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static void EnsureValidRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentException("Role is not valid.");
    }
}
