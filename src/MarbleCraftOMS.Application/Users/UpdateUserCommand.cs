namespace MarbleCraftOMS.Application.Users;

public record UpdateUserCommand(
    string Role,
    int? DistributorId,
    string? NewPassword);
