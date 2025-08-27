// UnitTests/TestKit/EntityNav/UserNav.cs
#nullable enable
namespace UnitTests.TestKit.EntityNav;

using Entities.Entites; // User, Role (and possibly UserRole)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public static class UserNav
{
    /// Initialize user role navigations for mapping code.
    /// Supports either:
    ///  - User.Roles : ICollection<Role>
    ///  - User.UserRoles : ICollection<UserRole> with a Role nav
    public static void Ensure(User u, params string[] roleNames)
    {
        // Try simple Roles collection first
        var rolesProp = typeof(User).GetProperty("Roles", BindingFlags.Public | BindingFlags.Instance);
        if (rolesProp is not null && rolesProp.PropertyType.IsGenericType)
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(Role)))!;
            foreach (var name in roleNames)
            {
                var r = new Role { Id = Guid.NewGuid(), Name = name };
                list.Add(r);
            }
            rolesProp.SetValue(u, list);
            return;
        }

        // Fallback: UserRoles join with Role nav
        var userRolesProp = typeof(User).GetProperty("UserRoles", BindingFlags.Public | BindingFlags.Instance);
        if (userRolesProp is not null && userRolesProp.PropertyType.IsGenericType)
        {
            var elementType = userRolesProp.PropertyType.GetGenericArguments()[0]; // e.g., UserRole
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
            foreach (var name in roleNames)
            {
                var ur = Activator.CreateInstance(elementType)!;
                elementType.GetProperty("UserId")?.SetValue(ur, u.Id);

                var role = new Role { Id = Guid.NewGuid(), Name = name };
                elementType.GetProperty("Role")?.SetValue(ur, role);
                elementType.GetProperty("RoleId")?.SetValue(ur, role.Id);

                list.Add(ur);
            }
            userRolesProp.SetValue(u, list);
        }
    }
}
