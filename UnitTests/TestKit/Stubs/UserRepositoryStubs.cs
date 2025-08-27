// UnitTests/TestKit/Stubs/UserRepositoryStubs.cs
#nullable enable
namespace UnitTests.TestKit.Stubs;
using Business.Services;
using Data.Repositories;       // IUserRepository
using Entities.Entites;        // User
using MockQueryable.Moq;
using Moq;
using UnitTests.TestKit.EntityNav;

public static class UserRepositoryStubs
{
    //public static Mock<IUserRepository> WithQueryable(this Mock<IUserRepository> repo, IEnumerable<User> users)
    //{
    //    var dbSetMock = users.AsQueryable().BuildMockDbSet(); // async-capable
    //    repo.SetupGet(r => r.Queryable).Returns(dbSetMock.Object);
    //    return repo;
    //}

    public static Mock<IUserRepository> WithQueryable(IEnumerable<User> users)
    {
        var dbSetMock = users.AsQueryable().BuildMockDbSet(); // async-capable DbSet<T>
        var repo = new Mock<IUserRepository>(MockBehavior.Strict);

        // DbSet<T> implements IQueryable<T>, so this satisfies your Queryable property
        repo.SetupGet(r => r.Queryable).Returns(dbSetMock.Object);

        repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        return repo;
    }

    public static void StubSetRolesNoop(this Mock<IUserRepository> repo)
        => repo.Setup(r => r.SetRolesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

    public static void StubAddEchoWithNavs(this Mock<IUserRepository> repo, Action<User>? onAdded = null)
    {
        repo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) =>
            {
                if (u.Id == Guid.Empty) u.Id = Guid.NewGuid();
                UserNav.Initialize(u, "User");
                onAdded?.Invoke(u);
                return u;
            });
    }

    public static void StubUpdateEchoWithNavs(this Mock<IUserRepository> repo)
        => repo.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((User u, CancellationToken _) =>
               {
                   UserNav.Initialize(u, "User");
                   return u;
               });

    public static void StubReloadWithRoles(this Mock<IUserRepository> repo, Func<Guid, User> factory)
        => repo.Setup(r => r.GetWithRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Guid id, CancellationToken _) => factory(id));

    public static UserService NewService(this Mock<IUserRepository> repo)
        => new(repo.Object);

    public static Mock<IUserRepository> StubEmailUnique(this Mock<IUserRepository> repo, bool exists = false)
    {
        repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
        return repo;
    }

    //public static Mock<IUserRepository> StubSetRolesNoop(this Mock<IUserRepository> repo)
    //{
    //    repo.Setup(r => r.SetRolesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
    //        .Returns(Task.CompletedTask);
    //    return repo;
    //}

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
