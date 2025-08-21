

namespace Business.Interfaces;

public interface IUserService
    : IBaseService<UserListItemDto, UserDetailDto, UserCreateDto, UserUpdateDto, Guid>
{
}
