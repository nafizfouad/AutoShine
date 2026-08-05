using AutoShine.Common;
using AutoShine.Service.DTOs.Schedules;
using AutoShine.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoShine.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _svc;
    public ProfileController(IProfileService svc) => _svc = svc;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _svc.GetProfileAsync(GetUserId());
        if (profile == null) return NotFound();
        return Ok(ApiResponse<object>.Ok(profile));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return BadRequest(ApiResponse<object>.Fail("First and last name are required."));
        var result = await _svc.UpdateProfileAsync(GetUserId(), dto);
        if (result == null) return NotFound();
        return Ok(ApiResponse<object>.Ok(result, "Profile updated."));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (dto.NewPassword.Length < 8)
            return BadRequest(ApiResponse<object>.Fail("New password must be at least 8 characters."));
        await _svc.ChangePasswordAsync(GetUserId(), dto);
        return Ok(ApiResponse<object>.Ok((object?)null, "Password changed successfully."));
    }
}
