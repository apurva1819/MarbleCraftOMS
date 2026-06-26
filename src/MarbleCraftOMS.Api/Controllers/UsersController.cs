using Asp.Versioning;
using MarbleCraftOMS.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Policy = "AdminOnly")]
[EnableRateLimiting("fixed")]
[ApiVersion("1.0")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await userService.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Create(CreateUserCommand cmd)
    {
        var user = await userService.CreateAsync(cmd);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Update(int id, UpdateUserCommand cmd)
    {
        var updated = await userService.UpdateAsync(id, cmd);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [EnableRateLimiting("fixed-write")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await userService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
