namespace MarbleCraftOMS.Application.Users;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserCommand cmd);
    Task<bool> UpdateAsync(int id, UpdateUserCommand cmd);
    Task<bool> DeleteAsync(int id);
}
