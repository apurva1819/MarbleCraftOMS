using System.Diagnostics;
using Asp.Versioning;
using MarbleCraftOMS.Application.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/suppliers")]
[Authorize]
[EnableRateLimiting("fixed")]
[ApiVersion("1.0")]
public class SuppliersController(ISupplierService supplierService, ILogger<SuppliersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sw = Stopwatch.StartNew();
        var suppliers = await supplierService.GetAllAsync();
        logger.LogInformation("Endpoint={Endpoint} ResultCount={Count} DurationMs={DurationMs}",
            nameof(GetAll), suppliers.Count, sw.ElapsedMilliseconds);
        return Ok(suppliers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var supplier = await supplierService.GetByIdAsync(id);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Add(AddSupplierCommand cmd)
    {
        var sw = Stopwatch.StartNew();
        var supplier = await supplierService.AddAsync(cmd);
        logger.LogInformation("Endpoint={Endpoint} SupplierId={SupplierId} DurationMs={DurationMs}",
            nameof(Add), supplier.Id, sw.ElapsedMilliseconds);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Update(int id, UpdateSupplierCommand cmd)
    {
        var sw = Stopwatch.StartNew();
        var updated = await supplierService.UpdateAsync(id, cmd);
        if (!updated) return NotFound();
        logger.LogInformation("Endpoint={Endpoint} SupplierId={SupplierId} DurationMs={DurationMs}",
            nameof(Update), id, sw.ElapsedMilliseconds);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Delete(int id)
    {
        var sw = Stopwatch.StartNew();
        var deleted = await supplierService.DeleteAsync(id);
        if (!deleted) return NotFound();
        logger.LogInformation("Endpoint={Endpoint} SupplierId={SupplierId} DurationMs={DurationMs}",
            nameof(Delete), id, sw.ElapsedMilliseconds);
        return NoContent();
    }
}
