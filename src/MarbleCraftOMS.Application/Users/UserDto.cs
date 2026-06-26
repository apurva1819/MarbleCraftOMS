namespace MarbleCraftOMS.Application.Users;

public record UserDto(
    int Id,
    string Username,
    string Role,
    int? DistributorId,
    DateTime CreatedAt);
