using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Core.Interfaces;

public interface IUserRepository
{
    Task<List<AppUser>> GetAllAsync();
    Task<AppUser?> GetByIdAsync(int id);
    Task<AppUser?> GetByUsernameAsync(string username);
    Task AddAsync(AppUser user);
    Task UpdateAsync(AppUser user);
    Task DeleteAsync(int id);
}
