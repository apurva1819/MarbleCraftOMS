using Asp.Versioning;
using MarbleCraftOMS.Application.Catalogue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/products")]
[Authorize]
[EnableRateLimiting("fixed")]
[ApiVersion("1.0")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;
        var result = await productService.BrowseAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await productService.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = "SalesAccess")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Add(AddProductCommand cmd)
    {
        var product = await productService.AddAsync(cmd);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SalesAccess")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Update(int id, UpdateProductCommand cmd)
    {
        var updated = await productService.UpdateAsync(id, cmd);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await productService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
