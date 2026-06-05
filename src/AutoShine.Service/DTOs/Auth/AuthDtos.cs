namespace AutoShine.Service.DTOs.Auth;

public record RegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Phone
);

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token,
    string Email,
    string FullName,
    string Role,
    int UserId,
    DateTime ExpiresAt
);
