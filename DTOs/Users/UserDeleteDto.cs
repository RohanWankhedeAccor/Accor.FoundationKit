// Common/DTOs/Users/UserDeleteDto.cs
namespace Common.DTOs.Users;

// Usually delete uses just the route id. If you want a body (for auditing/soft delete reason), use this:
public record UserDeleteDto(string? Reason);
