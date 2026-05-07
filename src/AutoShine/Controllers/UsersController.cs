using AutoShine.Common;
using AutoShine.Service.DTOs.Common;
using AutoShine.Service.DTOs.Users;
using AutoShine.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoShine.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Get paginated user list (Admin only). Filter by role or search by name/email.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? role = null,
        [FromQuery] string? search = null)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, role, search);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    /// <summary>Get a specific user by ID (Admin only).</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound(ApiResponse<UserDto>.Fail("User not found."));
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>Create a new employee or admin user (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto dto)
    {
        var user = await _userService.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ApiResponse<UserDto>.Ok(user, "User created."));
    }

    /// <summary>Update a user's profile (Admin only).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _userService.UpdateUserAsync(id, dto);
        if (user == null) return NotFound(ApiResponse<UserDto>.Fail("User not found."));
        return Ok(ApiResponse<UserDto>.Ok(user, "User updated."));
    }

    /// <summary>Soft-delete a user (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(int id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success) return NotFound(ApiResponse<bool>.Fail("User not found."));
        return Ok(ApiResponse<bool>.Ok(true, "User deactivated."));
    }
}
