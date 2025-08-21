
namespace Data.Repositories;

public class RoleRepository : EfBaseRepository<Role, Guid>, IRoleRepository
{
    private readonly AppDbContext _db;

    public RoleRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken ct)
        => await _db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
}
