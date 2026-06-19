using Asp.Versioning;
using MarbleCraftOMS.Application.Catalogue;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/products")]
[Authorize]
[EnableRateLimiting("fixed")]
[ApiVersion("1.0")]
public class ProductsController(IProductRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await repo.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await repo.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = "SalesAccess")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Add(AddProductCommand cmd)
    {
        var product = new Product
        {
            Name = cmd.Name,
            Material = cmd.Material,
            Format = cmd.Format,
            Surface = cmd.Surface,
            Color = cmd.Color,
            Size = cmd.Size,
            CountryOfOrigin = cmd.CountryOfOrigin,
            PricePerUnit = cmd.PricePerUnit,
            SupplierId = cmd.SupplierId
        };
        await repo.AddAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SalesAccess")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Update(int id, UpdateProductCommand cmd)
    {
        var product = await repo.GetByIdAsync(id);
        if (product is null) return NotFound();

        product.Name = cmd.Name;
        product.Material = cmd.Material;
        product.Format = cmd.Format;
        product.Surface = cmd.Surface;
        product.Color = cmd.Color;
        product.Size = cmd.Size;
        product.CountryOfOrigin = cmd.CountryOfOrigin;
        product.PricePerUnit = cmd.PricePerUnit;
        product.SupplierId = cmd.SupplierId;

        await repo.UpdateAsync(product);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await repo.GetByIdAsync(id);
        if (product is null) return NotFound();
        await repo.DeleteAsync(id);
        return NoContent();
    }
}
