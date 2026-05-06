using AutoShine.Common;
using AutoShine.Service.DTOs.Packages;
using AutoShine.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoShine.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackagesController : ControllerBase
{
    private readonly IPackageService _packageService;

    public PackagesController(IPackageService packageService)
    {
        _packageService = packageService;
    }

    /// <summary>Get all active packages (public).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PackageDto>>>> GetPackages(
        [FromQuery] bool activeOnly = true)
    {
        var packages = await _packageService.GetAllPackagesAsync(activeOnly);
        return Ok(ApiResponse<IEnumerable<PackageDto>>.Ok(packages));
    }

    /// <summary>Get a specific package by ID (public).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PackageDto>>> GetPackage(int id)
    {
        var pkg = await _packageService.GetPackageByIdAsync(id);
        if (pkg == null) return NotFound(ApiResponse<PackageDto>.Fail("Package not found."));
        return Ok(ApiResponse<PackageDto>.Ok(pkg));
    }

    /// <summary>Create a new service package (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PackageDto>>> CreatePackage([FromBody] CreatePackageDto dto)
    {
        var pkg = await _packageService.CreatePackageAsync(dto);
        return CreatedAtAction(nameof(GetPackage), new { id = pkg.Id }, ApiResponse<PackageDto>.Ok(pkg, "Package created."));
    }

    /// <summary>Update a service package and its recipe items (Admin only).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PackageDto>>> UpdatePackage(int id, [FromBody] UpdatePackageDto dto)
    {
        var pkg = await _packageService.UpdatePackageAsync(id, dto);
        if (pkg == null) return NotFound(ApiResponse<PackageDto>.Fail("Package not found."));
        return Ok(ApiResponse<PackageDto>.Ok(pkg, "Package updated."));
    }

    /// <summary>Soft-delete a package (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePackage(int id)
    {
        var success = await _packageService.DeletePackageAsync(id);
        if (!success) return NotFound(ApiResponse<bool>.Fail("Package not found."));
        return Ok(ApiResponse<bool>.Ok(true, "Package deactivated."));
    }
}
