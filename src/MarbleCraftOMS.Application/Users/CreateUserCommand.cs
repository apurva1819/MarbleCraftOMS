namespace MarbleCraftOMS.Application.Users;

public record CreateUserCommand(
    string Username,
    string Password,
    string Role,
    int? DistributorId);
