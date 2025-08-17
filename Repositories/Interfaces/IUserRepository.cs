// Data/Repositories/IUserRepository.cs
using Entities.Entites;

namespace Data.Repositories;

public interface IUserRepository : IBaseRepository<User, Guid>
{
    Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct);
    Task SetRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken ct);

    /// <summary>Loads a user with Roles included (or null if not found).</summary>
    Task<User?> GetWithRolesAsync(Guid id, CancellationToken ct);
}
