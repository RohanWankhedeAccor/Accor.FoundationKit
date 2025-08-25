
namespace Data.Repositories;

public class RoleRepository : EfBaseRepository<Role, Guid>, IRoleRepository
{
    public RoleRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken ct)
        => await _db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
}
