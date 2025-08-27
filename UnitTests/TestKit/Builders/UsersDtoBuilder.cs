// UnitTests/TestKit/Builders/UsersDtoBuilder.cs
#nullable enable
namespace UnitTests.TestKit.Builders;

using Common.DTOs.Users;   // User* DTOs
using System;
using System.Collections.Generic;

public static class UsersDtoBuilder
{
    public static UserCreateDto NewCreate(
        string first = "Jane",
        string last = "Doe",
        string email = "jane@example.com",
        bool active = true,
        IReadOnlyList<Guid>? roleIds = null)
        => new(first, last, email, active, roleIds ?? Array.Empty<Guid>());

    public static UserUpdateDto NewUpdate(
        string first = "New",
        string last = "Name",
        string email = "new@example.com",
        bool active = true,
        IReadOnlyList<Guid>? roleIds = null)
        => new(first, last, email, active, roleIds ?? Array.Empty<Guid>());

    public static UserDetailDto MakeDetail(
        Guid id,
        string first,
        string last,
        string email,
        bool active,
        DateTime created,
        DateTime? updated,
        List<RoleItemDto> roles)
        => new(id, first, last, email, active, created, updated, roles);

    public static UserListItemDto MakeListItem(
        Guid id,
        string first,
        string last,
        string email,
        bool active,
        List<RoleItemDto> roles)
        => new(id, first, last, email, active, roles);
}
