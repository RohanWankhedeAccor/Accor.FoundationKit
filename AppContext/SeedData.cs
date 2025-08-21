
namespace AppContext
{
    public static class SeedData
    {
        private static readonly string[] DefaultRoles =
        {
            "Super Admin",
            "Admin",
            "User",
            "Viewer",
            "Tester"
        };

        public static async Task EnsureSeedRolesAsync(AppDbContext db, CancellationToken ct = default)
        {
            // Get the roles that already exist (case-insensitive match)
            var existing = await db.Roles
                .Select(r => r.Name)
                .ToListAsync(ct);

            var toAdd = DefaultRoles
                .Where(rn => !existing.Any(e => string.Equals(e, rn, StringComparison.OrdinalIgnoreCase)))
                .Select(rn => new Role
                {
                    Id = Guid.NewGuid(),   // assumes Id is Guid from BaseEntity
                    Name = rn
                })
                .ToList();

            if (toAdd.Count > 0)
            {
                db.Roles.AddRange(toAdd);
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
