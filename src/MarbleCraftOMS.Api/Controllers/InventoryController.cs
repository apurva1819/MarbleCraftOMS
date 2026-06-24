using System.Security.Claims;
using Asp.Versioning;
using MarbleCraftOMS.Application.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/inventory")]
[Authorize]
[EnableRateLimiting("fixed")]
[ApiVersion("1.0")]
public class InventoryController(
    IInventoryService inventoryService,
    ILogger<InventoryController> logger) : ControllerBase
{
    [HttpGet("{sku:int}")]
    public async Task<IActionResult> GetBySku(int sku)
    {
        var result = await inventoryService.GetBySkuAsync(sku);
        return result is null
            ? NotFound(new { message = $"No stock lots found for SKU {sku}." })
            : Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var items = await inventoryService.GetSummaryAsync();
        return Ok(items);
    }

    [HttpPost("adjust")]
    [Authorize(Policy = "SalesAccess")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Adjust(AdjustStockCommand cmd)
    {
        try
        {
            var result = await inventoryService.AdjustAsync(cmd);
            if (result is null)
                return NotFound(new { message = $"Stock lot {cmd.StockLotId} not found." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.Identity?.Name ?? "unknown";
            logger.LogInformation(
                "Inventory adjusted — Lot={LotNumber} Type={Type} Qty={Qty} Reason={Reason} User={UserId}",
                result.LotNumber, cmd.Type, cmd.Quantity, cmd.Reason, userId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}
