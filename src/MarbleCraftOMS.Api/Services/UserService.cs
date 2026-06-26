using MarbleCraftOMS.Application.Users;
using MarbleCraftOMS.Core.Constants;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;

namespace MarbleCraftOMS.Api.Services;

public class UserService(IUserRepository repo) : IUserService
{
    private static readonly string[] ValidRoles =
        [Roles.Admin, Roles.SalesAgent, Roles.WarehouseStaff, Roles.Distributor];

    public async Task<List<UserDto>> GetAllAsync() =>
        (await repo.GetAllAsync()).Select(ToDto).ToList();

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await repo.GetByIdAsync(id);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserCommand cmd)
    {
        if (!ValidRoles.Contains(cmd.Role))
            throw new ArgumentException($"Role '{cmd.Role}' is not valid.", nameof(cmd.Role));

        var existing = await repo.GetByUsernameAsync(cmd.Username);
        if (existing is not null)
            throw new InvalidOperationException($"Username '{cmd.Username}' is already taken.");

        var user = new AppUser
        {
            Username      = cmd.Username,
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(cmd.Password),
            Role          = cmd.Role,
            DistributorId = cmd.DistributorId
        };
        await repo.AddAsync(user);
        return ToDto(user);
    }

    public async Task<bool> UpdateAsync(int id, UpdateUserCommand cmd)
    {
        var user = await repo.GetByIdAsync(id);
        if (user is null) return false;

        if (!ValidRoles.Contains(cmd.Role))
            throw new ArgumentException($"Role '{cmd.Role}' is not valid.", nameof(cmd.Role));

        user.Role          = cmd.Role;
        user.DistributorId = cmd.DistributorId;

        if (!string.IsNullOrWhiteSpace(cmd.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.NewPassword);

        await repo.UpdateAsync(user);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await repo.GetByIdAsync(id);
        if (user is null) return false;
        await repo.DeleteAsync(id);
        return true;
    }

    private static UserDto ToDto(AppUser u) =>
        new(u.Id, u.Username, u.Role, u.DistributorId, u.CreatedAt);
}
