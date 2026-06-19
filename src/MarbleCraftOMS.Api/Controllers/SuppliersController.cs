using Asp.Versioning;
using MarbleCraftOMS.Application.Suppliers;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/suppliers")]
[Authorize]
[EnableRateLimiting("fixed")]
[ApiVersion("1.0")]
public class SuppliersController(ISupplierRepository repo, ILogger<SuppliersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sw = Stopwatch.StartNew();
        var suppliers = await repo.GetAllAsync();
        logger.LogInformation("Endpoint={Endpoint} ResultCount={Count} DurationMs={DurationMs}",
            nameof(GetAll), suppliers.Count, sw.ElapsedMilliseconds);
        return Ok(suppliers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var supplier = await repo.GetByIdAsync(id);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Add(AddSupplierCommand cmd)
    {
        var sw = Stopwatch.StartNew();
        var supplier = new Supplier
        {
            CompanyName = cmd.CompanyName,
            ContactName = cmd.ContactName,
            ContactPhone = cmd.ContactPhone,
            ContactEmail = cmd.ContactEmail,
            Address = cmd.Address,
            Country = cmd.Country,
            Specialisation = cmd.Specialisation
        };
        await repo.AddAsync(supplier);
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
        var supplier = await repo.GetByIdAsync(id);
        if (supplier is null) return NotFound();

        supplier.CompanyName = cmd.CompanyName;
        supplier.ContactName = cmd.ContactName;
        supplier.ContactPhone = cmd.ContactPhone;
        supplier.ContactEmail = cmd.ContactEmail;
        supplier.Address = cmd.Address;
        supplier.Country = cmd.Country;
        supplier.Specialisation = cmd.Specialisation;

        await repo.UpdateAsync(supplier);
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
        var supplier = await repo.GetByIdAsync(id);
        if (supplier is null) return NotFound();
        await repo.DeleteAsync(id);
        logger.LogInformation("Endpoint={Endpoint} SupplierId={SupplierId} DurationMs={DurationMs}",
            nameof(Delete), id, sw.ElapsedMilliseconds);
        return NoContent();
    }
}
