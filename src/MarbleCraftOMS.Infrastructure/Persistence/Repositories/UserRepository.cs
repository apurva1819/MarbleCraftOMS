using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<List<AppUser>> GetAllAsync() =>
        await db.Users.ToListAsync();

    public async Task<AppUser?> GetByIdAsync(int id) =>
        await db.Users.FindAsync(id);

    public async Task<AppUser?> GetByUsernameAsync(string username) =>
        await db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task AddAsync(AppUser user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppUser user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is not null)
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
    }
}
