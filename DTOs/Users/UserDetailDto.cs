// Common/DTOs/Users/UserDetailDto.cs
namespace Common.DTOs.Users;

public record UserDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool Active,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    IReadOnlyList<RoleItemDto> Roles    // names + ids
);
