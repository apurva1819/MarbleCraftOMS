using Asp.Versioning;
using MarbleCraftOMS.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/orders")]
[Authorize]
[EnableRateLimiting("fixed")]
[ApiVersion("1.0")]
public class OrdersController(
    IOrderService orderService,
    ILogger<OrdersController> logger) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Place([FromBody] PlaceOrderCommand cmd)
    {
        var result = await orderService.PlaceAsync(cmd);
        logger.LogInformation("Order placed — OrderNumber={OrderNumber} Customer={CustomerId}",
            result.OrderNumber, cmd.CustomerId);
        return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? customerId)
    {
        var orders = customerId.HasValue
            ? await orderService.GetByCustomerAsync(customerId.Value)
            : await orderService.GetAllAsync();
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await orderService.GetByIdAsync(id);
        return order is null
            ? NotFound(new { message = $"Order {id} not found." })
            : Ok(order);
    }

    [HttpPatch("{id:int}/confirm")]
    [Authorize(Policy = "SalesAccess")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Confirm(int id)
    {
        await orderService.ConfirmAsync(id);
        logger.LogInformation("Order confirmed — OrderId={OrderId}", id);
        return NoContent();
    }

    [HttpPatch("{id:int}/dispatch")]
    [Authorize(Policy = "SalesAccess")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Dispatch(int id)
    {
        await orderService.DispatchAsync(id);
        logger.LogInformation("Order dispatched — OrderId={OrderId}", id);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "SalesAccess")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Cancel(int id)
    {
        await orderService.CancelAsync(id);
        logger.LogInformation("Order cancelled — OrderId={OrderId}", id);
        return NoContent();
    }
}
