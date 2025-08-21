
namespace Common.DTOs.Users;

public record UserUpdateDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    bool Active,
    IReadOnlyList<Guid>? RoleIds = null  // optional; null = keep existing; [] = remove all
);
