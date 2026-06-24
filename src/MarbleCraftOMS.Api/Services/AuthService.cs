using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MarbleCraftOMS.Application.Auth;
using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MarbleCraftOMS.Api.Services;

public class AuthService(AppDbContext db, IConfiguration config) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginCommand cmd)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == cmd.Username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
            return null;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("distributorId", user.DistributorId?.ToString() ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var expiry = DateTime.UtcNow.AddMinutes(
            double.Parse(config["Jwt:ExpiryMinutes"] ?? "60"));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiry,
            Role = user.Role
        };
    }
}
