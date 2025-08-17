namespace Common.DTOs.Users;

public record RoleItemDto(Guid Id, string Name);                 // for lists
public record RoleDetailDto(Guid Id, string Name);               // for single get
public record RoleCreateDto(string Name);                        // not used (blocked in service)
public record RoleUpdateDto(string Name);