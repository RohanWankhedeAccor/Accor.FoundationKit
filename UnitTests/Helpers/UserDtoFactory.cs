using Common.DTOs.Users; // UserCreateDto, UserUpdateDto

namespace UnitTests.Helpers
{
    public static class UserDtoFactory
    {
        public static UserCreateDto NewCreateDto(
            string first = "Jane", string last = "Doe",
            string email = "jane@example.com",
            bool active = true,
            IReadOnlyList<Guid>? roleIds = null)
            => new(first, last, email, active, roleIds ?? Array.Empty<Guid>());

        public static UserUpdateDto NewUpdateDto(
            string first = "New", string last = "Name",
            string email = "new@example.com",
            bool active = true,
            IReadOnlyList<Guid>? roleIds = null)
            => new(first, last, email, active, roleIds ?? Array.Empty<Guid>());
    }
}
