
namespace Common.DTOs.Users;

public record UserCreateDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    bool Active = true,
    IReadOnlyList<Guid>? RoleIds = null  // optional; null = no roles; [] = no roles
);
