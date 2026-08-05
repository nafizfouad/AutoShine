using AutoShine.Common;
using AutoShine.Service.DTOs.Schedules;
using AutoShine.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoShine.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleService _svc;
    public ScheduleController(IScheduleService svc) => _svc = svc;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Templates ────────────────────────────────────────────────────────────

    [HttpGet("templates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllTemplates()
        => Ok(ApiResponse<object>.Ok(await _svc.GetAllTemplatesAsync()));

    [HttpGet("templates/employee/{employeeId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
        => Ok(ApiResponse<object>.Ok(await _svc.GetTemplatesByEmployeeAsync(employeeId)));

    [HttpPost("templates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateScheduleTemplateDto dto)
    {
        var result = await _svc.CreateTemplateAsync(dto);
        return CreatedAtAction(nameof(GetAllTemplates), ApiResponse<object>.Ok(result, "Template created."));
    }

    [HttpDelete("templates/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var ok = await _svc.DeleteTemplateAsync(id);
        if (!ok) return NotFound(ApiResponse<bool>.Fail("Template not found."));
        return Ok(ApiResponse<bool>.Ok(true, "Template deleted."));
    }

    // ── Leaves ───────────────────────────────────────────────────────────────

    [HttpGet("leaves")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllLeaves()
        => Ok(ApiResponse<object>.Ok(await _svc.GetAllLeavesAsync()));

    [HttpGet("leaves/employee/{employeeId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLeavesByEmployee(int employeeId)
        => Ok(ApiResponse<object>.Ok(await _svc.GetLeavesByEmployeeAsync(employeeId)));

    [HttpPost("leaves")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateLeave([FromBody] CreateLeaveDto dto)
    {
        var result = await _svc.CreateLeaveAsync(dto);
        return CreatedAtAction(nameof(GetAllLeaves), ApiResponse<object>.Ok(result, "Leave recorded."));
    }

    [HttpDelete("leaves/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLeave(int id)
    {
        var ok = await _svc.DeleteLeaveAsync(id);
        if (!ok) return NotFound(ApiResponse<bool>.Fail("Leave not found."));
        return Ok(ApiResponse<bool>.Ok(true, "Leave removed."));
    }
}
