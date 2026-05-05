using AutoShine.Common;
using AutoShine.Service.DTOs.Auth;
using AutoShine.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutoShine.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Register a new customer account.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Registration successful."));
    }

    /// <summary>Login and receive a JWT token.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
    }
}
