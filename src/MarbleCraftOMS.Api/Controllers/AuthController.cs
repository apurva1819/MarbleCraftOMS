using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using MarbleCraftOMS.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarbleCraftOMS.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}")]
[ApiVersion("1.0")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health() =>
        Ok(new { status = "healthy", timestamp = DateTime.UtcNow });

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand cmd)
    {
        var result = await authService.LoginAsync(cmd);
        if (result is null)
            return Unauthorized(new { message = "Invalid username or password." });
        return Ok(result);
    }

    // Test endpoint — verifies 401 (no token), 200 (valid local JWT)
    // For 403: hit any [Authorize(Policy="AdminOnly")] endpoint with a salesagent token
    [Authorize(AuthenticationSchemes = "Local")]
    [HttpGet("me-local")]
    public IActionResult MeLocal() =>
        Ok(new
        {
            message = "Authenticated via local JWT",
            userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            role = User.FindFirst(ClaimTypes.Role)?.Value,
            distributorId = User.FindFirst("distributorId")?.Value
        });

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
