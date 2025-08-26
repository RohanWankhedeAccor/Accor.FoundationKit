using Entities.Entites; // User, Role
using System.Collections;
using System.Reflection;

namespace UnitTests.Helpers
{
    public static class UserNavHelper
    {
        /// Ensures User navigations are non-null for mapping.
        /// Supports either:
        ///  - User.Roles : ICollection<Role>
        ///  - User.UserRoles : ICollection<UserRole> with a .Role nav
        public static void EnsureUserNavs(User u, params string[] roleNames)
        {
            // Try simple Roles collection first
            var rolesProp = typeof(User).GetProperty("Roles", BindingFlags.Public | BindingFlags.Instance);
            if (rolesProp is not null && rolesProp.PropertyType.IsGenericType)
            {
                var roleType = typeof(Role);
                var listType = typeof(List<>).MakeGenericType(roleType);
                var list = (IList)Activator.CreateInstance(listType)!;

                foreach (var name in roleNames)
                {
                    var r = (Role)Activator.CreateInstance(roleType)!;
                    r.Id = Guid.NewGuid();
                    r.Name = name;
                    list.Add(r);
                }
                rolesProp.SetValue(u, list);
                return;
            }

            // Fallback: UserRoles join type with Role nav
            var userRolesProp = typeof(User).GetProperty("UserRoles", BindingFlags.Public | BindingFlags.Instance);
            if (userRolesProp is not null && userRolesProp.PropertyType.IsGenericType)
            {
                var roleType = typeof(Role);
                var elementType = userRolesProp.PropertyType.GetGenericArguments()[0]; // e.g., UserRole
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (IList)Activator.CreateInstance(listType)!;

                foreach (var name in roleNames)
                {
                    var userRole = Activator.CreateInstance(elementType)!;

                    elementType.GetProperty("UserId")?.SetValue(userRole, u.Id);

                    var role = (Role)Activator.CreateInstance(roleType)!;
                    role.Id = Guid.NewGuid();
                    role.Name = name;

                    elementType.GetProperty("Role")?.SetValue(userRole, role);
                    elementType.GetProperty("RoleId")?.SetValue(userRole, role.Id);

                    list.Add(userRole);
                }

                userRolesProp.SetValue(u, list);
            }
        }
    }
}
