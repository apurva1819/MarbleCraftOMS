using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() =>
        Ok(new { status = "healthy", timestamp = DateTime.UtcNow });

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() =>
        Ok(new
        {
            message = "Authenticated via Entra ID — zero secrets used",
            user = User.Identity?.Name,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
}
