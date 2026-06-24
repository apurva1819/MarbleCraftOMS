namespace MarbleCraftOMS.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginCommand cmd);
}
