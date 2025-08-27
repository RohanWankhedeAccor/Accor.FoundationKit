// file: UnitTests/Services/UserServiceTests.cs
#nullable enable
namespace UnitTests.Services;

using Common.DTOs.Paging;
using Entities.Entites;
using FluentAssertions;
using Moq;
using UnitTests.TestKit.Builders;   // UsersDtoBuilder
using UnitTests.TestKit.EntityNav;  // UserNav
using UnitTests.TestKit.Stubs;      // UserRepositoryStubs

public sealed class UserServiceTests
{
    [Fact]
    public async Task ListPagedAsync_returns_expected_page_and_total()
    {
        // Arrange: 23 users with safe (non-null) navs
        var users = Enumerable.Range(0, 23)
            .Select(i =>
            {
                var u = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = $"First{i:D2}",
                    LastName = "X",
                    Email = $"u{i}@ex.com",
                    Active = true
                };
                UserNav.Initialize(u); // initialize roles navs
                return u;
            })
            .ToList();

        var repo = UserRepositoryStubs.WithQueryable(users);
        var svc = repo.NewService();

        var req = new PagingRequest { Page = 2, PageSize = 5 };

        // Act
        var result = await svc.ListPagedAsync(req, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(23);
        result.Items.Count.Should().Be(5);
    }

    [Fact]
    public async Task CreateAsync_calls_AddAsync_and_returns_created()
    {
        // Arrange
        var repo = new Mock<Data.Repositories.IUserRepository>(MockBehavior.Strict);

        repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // no-op role writer
        repo.StubSetRolesNoop();

        // capture the added entity
        User? added = null;
        repo.StubAddEchoWithNavs(u => added = u);

        // service reload after create
        repo.StubReloadWithRoles(id =>
        {
            var u = new User
            {
                Id = id,
                FirstName = added?.FirstName ?? "Jane",
                LastName = added?.LastName ?? "Doe",
                Email = added?.Email ?? "jane@example.com",
                Active = true
            };
            UserNav.Ensure(u, "User");
            return u;
        });

        var svc = repo.NewService();

        // IMPORTANT: provide roleIds so SetRolesAsync is invoked
        var roleId = Guid.NewGuid();
        var dto = UsersDtoBuilder.NewCreate(
            "Jane", "Doe", "jane@example.com", true, new[] { roleId });

        // Act
        var created = await svc.CreateAsync(dto, CancellationToken.None);

        // Assert
        created.Should().NotBeNull();
        created.Email.Should().Be("jane@example.com");

        repo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SetRolesAsync(created.Id, It.Is<IEnumerable<Guid>>(ids => ids.Contains(roleId)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_returns_null_when_user_not_found()
    {
        // Arrange
        var repo = new Mock<Data.Repositories.IUserRepository>(MockBehavior.Strict);

        // email uniqueness check always happens first
        repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // then the lookup returns null
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var svc = repo.NewService();
        var dto = UsersDtoBuilder.NewUpdate("John", "Smith", "john@example.com");

        // Act
        var result = await svc.UpdateAsync(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        repo.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_updates_and_returns_value_when_found()
    {
        var existing = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com",
            Active = true
        };
        UserNav.Initialize(existing, "User");

        var repo = new Mock<Data.Repositories.IUserRepository>(MockBehavior.Strict);

        repo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repo.StubSetRolesNoop();
        repo.StubUpdateEchoWithNavs();

        repo.Setup(r => r.GetWithRolesAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                var u = new User
                {
                    Id = id,
                    FirstName = "New",
                    LastName = "Name",
                    Email = "new@example.com",
                    Active = true
                };
                UserNav.Initialize(u, "User");
                return u;
            });

        var svc = repo.NewService();
        var dto = UsersDtoBuilder.NewUpdate("New", "Name", "new@example.com");

        var result = await svc.UpdateAsync(existing.Id, dto, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("new@example.com");
        repo.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SetRolesAsync(existing.Id, It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_returns_true_when_deleted_and_false_when_missing()
    {
        var existingId = Guid.NewGuid();

        var repo = new Mock<Data.Repositories.IUserRepository>(MockBehavior.Strict);

        repo.Setup(r => r.DeleteAsync(existingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        repo.Setup(r => r.DeleteAsync(It.Is<Guid>(g => g != existingId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = repo.NewService();

        (await svc.DeleteAsync(existingId, CancellationToken.None)).Should().BeTrue();
        (await svc.DeleteAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeFalse();

        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
