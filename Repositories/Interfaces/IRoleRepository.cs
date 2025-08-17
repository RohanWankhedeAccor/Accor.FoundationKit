using Entities.Entites;

namespace Data.Repositories
{
    public interface IRoleRepository : IBaseRepository<Role, Guid>
    {
        Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct);
    }
}
