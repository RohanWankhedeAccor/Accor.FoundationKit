namespace Data.Repositories;

public class UserRepository : EfBaseRepository<User, Guid>, IUserRepository
{
    private readonly DbSet<Role> _roles;
    private readonly DbSet<UserRole> _userRoles;

    public UserRepository(AppDbContext db) : base(db)
    {
        _roles = db.Set<Role>();
        _userRoles = db.Set<UserRole>();
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        return await _set.AnyAsync(
            u => !u.IsDeleted &&
                 u.Email.ToLower() == email &&
                 (excludeId == null || u.Id != excludeId.Value),
            ct);
    }

    public async Task<User?> GetWithRolesAsync(Guid id, CancellationToken ct)
    {
        return await _set
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
    }

    public async Task SetRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken ct)
    {
        var user = await _set
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);

        if (user is null) return;

        var desired = new HashSet<Guid>(roleIds ?? Array.Empty<Guid>());

        // Remove roles no longer desired
        var toRemove = user.UserRoles.Where(ur => !desired.Contains(ur.RoleId)).ToList();
        if (toRemove.Count > 0)
            _userRoles.RemoveRange(toRemove);

        // Add missing roles (only if they exist)
        var existingRoleIds = await _roles
            .Where(r => desired.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(ct);

        var current = new HashSet<Guid>(user.UserRoles.Select(ur => ur.RoleId));
        var toAddIds = existingRoleIds.Where(id => !current.Contains(id)).ToList();

        foreach (var rid in toAddIds)
            user.UserRoles.Add(new UserRole { UserId = userId, RoleId = rid });

        await _db.SaveChangesAsync(ct);
    }
}
