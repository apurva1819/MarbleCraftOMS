using Asp.Versioning;
using MarbleCraftOMS.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
[EnableRateLimiting("fixed")]
[ApiVersion("1.0")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20)
    {
        // Distributors see their own notifications; internal staff see broadcasts
        var customerId = ResolveCustomerId();
        var results = await notificationService.GetRecentAsync(customerId, count);
        return Ok(results);
    }

    [HttpPatch("{id:int}/read")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> MarkRead(int id)
    {
        await notificationService.MarkReadAsync(id);
        return NoContent();
    }

    [HttpPatch("read-all")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> MarkAllRead()
    {
        var customerId = ResolveCustomerId();
        await notificationService.MarkAllReadAsync(customerId);
        return NoContent();
    }

    // Distributors have a "distributorId" int claim that maps to Customer.Id.
    // Internal staff have no distributorId claim → null → they see all broadcasts.
    private int? ResolveCustomerId()
    {
        var claim = User.FindFirst("distributorId")?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }
}
