using AutoShine.Common;
using AutoShine.Service.DTOs.Schedules;
using AutoShine.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoShine.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _svc;
    public ProfileController(IProfileService svc) => _svc = svc;

    private int GetUserId() => User.GetUserId();

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
        var result = await _svc.UpdateProfileAsync(GetUserId(), dto);
        if (result == null) return NotFound();
        return Ok(ApiResponse<object>.Ok(result, "Profile updated."));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!await _svc.ChangePasswordAsync(GetUserId(), dto)) return NotFound();
        return Ok(ApiResponse<bool>.Ok(true, "Password changed successfully."));
    }
}
