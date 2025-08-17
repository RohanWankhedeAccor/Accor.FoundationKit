// Business/Interfaces/IUserService.cs
using Common.DTOs.Users;

namespace Business.Interfaces;

public interface IUserService
    : IBaseService<UserListItemDto, UserDetailDto, UserCreateDto, UserUpdateDto, Guid>
{
}
