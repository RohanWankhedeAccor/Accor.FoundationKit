// UnitTests/TestKit/Stubs/UserRepositoryStubs.cs
#nullable enable
namespace UnitTests.TestKit.Stubs;

using Data.Repositories;       // IUserRepository
using Entities.Entites;        // User
using MockQueryable.Moq;       // BuildMockDbSet
using Moq;

public static class UserRepositoryStubs
{
    public static Mock<IUserRepository> WithQueryable(this Mock<IUserRepository> repo, IEnumerable<User> users)
    {
        var dbSetMock = users.AsQueryable().BuildMockDbSet(); // async-capable
        repo.SetupGet(r => r.Queryable).Returns(dbSetMock.Object);
        return repo;
    }

    public static Mock<IUserRepository> StubEmailUnique(this Mock<IUserRepository> repo, bool exists = false)
    {
        repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
        return repo;
    }

    public static Mock<IUserRepository> StubSetRolesNoop(this Mock<IUserRepository> repo)
    {
        repo.Setup(r => r.SetRolesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return repo;
    }

    /// Echoes the entity back, assigns Id if empty, and ensures navs.
    public static Mock<IUserRepository> StubAddEcho(this Mock<IUserRepository> repo, Action<User>? onAdded = null)
    {
        repo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) =>
            {
                if (u.Id == Guid.Empty) u.Id = Guid.NewGuid();
                UnitTests.TestKit.EntityNav.UserNav.Ensure(u, "User");
                onAdded?.Invoke(u);
                return u;
            });
        return repo;
    }

    public static Mock<IUserRepository> StubUpdateEcho(this Mock<IUserRepository> repo)
    {
        repo.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) =>
            {
                UnitTests.TestKit.EntityNav.UserNav.Ensure(u, "User");
                return u;
            });
        return repo;
    }

    public static Mock<IUserRepository> StubGetWithRoles(this Mock<IUserRepository> repo, Func<Guid, User?> loader)
    {
        repo.Setup(r => r.GetWithRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => loader(id));
        return repo;
    }
}
