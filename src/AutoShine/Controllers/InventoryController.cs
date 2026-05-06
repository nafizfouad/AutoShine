using AutoShine.Common;
using AutoShine.Service.DTOs.Common;
using AutoShine.Service.DTOs.Inventory;
using AutoShine.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoShine.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>Get paginated inventory. Use lowStockOnly=true for alert view.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryItemDto>>>> GetInventory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? lowStockOnly = null)
    {
        var result = await _inventoryService.GetInventoryAsync(page, pageSize, lowStockOnly);
        return Ok(ApiResponse<PagedResult<InventoryItemDto>>.Ok(result));
    }

    /// <summary>Get all items with stock at or below threshold (alert dashboard).</summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryItemDto>>>> GetLowStockAlerts()
    {
        var alerts = await _inventoryService.GetLowStockAlertsAsync();
        return Ok(ApiResponse<IEnumerable<InventoryItemDto>>.Ok(alerts));
    }

    /// <summary>Get a specific inventory item by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InventoryItemDto>>> GetItem(int id)
    {
        var item = await _inventoryService.GetItemByIdAsync(id);
        if (item == null) return NotFound(ApiResponse<InventoryItemDto>.Fail("Item not found."));
        return Ok(ApiResponse<InventoryItemDto>.Ok(item));
    }

    /// <summary>Create a new inventory item.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<InventoryItemDto>>> CreateItem([FromBody] CreateInventoryItemDto dto)
    {
        var item = await _inventoryService.CreateItemAsync(dto);
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, ApiResponse<InventoryItemDto>.Ok(item, "Item created."));
    }

    /// <summary>Update an existing inventory item.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<InventoryItemDto>>> UpdateItem(int id, [FromBody] UpdateInventoryItemDto dto)
    {
        var item = await _inventoryService.UpdateItemAsync(id, dto);
        if (item == null) return NotFound(ApiResponse<InventoryItemDto>.Fail("Item not found."));
        return Ok(ApiResponse<InventoryItemDto>.Ok(item, "Item updated."));
    }

    /// <summary>Delete an inventory item.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteItem(int id)
    {
        var success = await _inventoryService.DeleteItemAsync(id);
        if (!success) return NotFound(ApiResponse<bool>.Fail("Item not found."));
        return Ok(ApiResponse<bool>.Ok(true, "Item deleted."));
    }
}
