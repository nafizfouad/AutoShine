using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.Common;
using AutoShine.Service.DTOs.Auth;
using AutoShine.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AutoShine.Service.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration config)
    {
        _unitOfWork = unitOfWork;
        _config = config;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var firstName = Validate.Name(dto.FirstName, "First name");
        var lastName = Validate.Name(dto.LastName, "Last name");
        var email = Validate.Email(dto.Email);
        var phone = Validate.Phone(dto.Phone, required: true);
        Validate.Password(dto.Password);

        var existing = await _unitOfWork.Users.GetByEmailAsync(email);
        if (existing != null)
            throw new InvalidOperationException("Email is already registered.");

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = phone,
            Role = UserRole.Customer
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrEmpty(dto.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        return GenerateAuthResponse(user);
    }

    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var token = GenerateJwtToken(user);
        var expiry = DateTime.UtcNow.AddHours(
            double.Parse(_config["Jwt:ExpiryHours"] ?? "24"));

        return new AuthResponseDto(
            token,
            user.Email,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}",
            user.Role.ToString(),
            user.Id,
            expiry
        );
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(
                double.Parse(_config["Jwt:ExpiryHours"] ?? "24")),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
