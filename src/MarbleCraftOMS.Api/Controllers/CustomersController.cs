using Asp.Versioning;
using MarbleCraftOMS.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/customers")]
[Authorize]
[EnableRateLimiting("fixed")]
[ApiVersion("1.0")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await customerService.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await customerService.GetByIdAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Add(AddCustomerCommand cmd)
    {
        var customer = await customerService.AddAsync(cmd);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Update(int id, UpdateCustomerCommand cmd)
    {
        var updated = await customerService.UpdateAsync(id, cmd);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await customerService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
