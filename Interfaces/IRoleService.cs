using Common.DTOs.Users;

namespace Business.Interfaces;

public interface IRoleService
    : IBaseService<RoleItemDto, RoleDetailDto, RoleCreateDto, RoleUpdateDto, Guid>
{
}
