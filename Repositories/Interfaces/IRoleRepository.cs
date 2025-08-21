
namespace Data.Repositories
{
    public interface IRoleRepository : IBaseRepository<Role, Guid>
    {
        Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken ct);
    }
}
