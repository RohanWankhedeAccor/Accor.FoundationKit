// Common/DTOs/Users/UserListItemDto.cs
namespace Common.DTOs.Users;

public record UserListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool Active,
    IReadOnlyList<RoleItemDto> Roles
);
