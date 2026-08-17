using AutoShine.Models.Enums;

namespace AutoShine.Service.DTOs.Users;

public record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    bool IsActive
);

public record CreateUserDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Phone,
    UserRole Role
);

public record UpdateUserDto(
    string FirstName,
    string LastName,
    string Phone,
    bool IsActive
);
