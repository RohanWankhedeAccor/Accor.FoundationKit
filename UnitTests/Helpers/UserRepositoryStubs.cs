using Business.Services;    // UserService
using Data.Repositories;    // IUserRepository
using Entities.Entites;     // User
using MockQueryable.Moq;
using Moq;

namespace UnitTests.Helpers
{
    public static class UserRepositoryStubs
    {
        public static Mock<IUserRepository> MockRepoWithQueryable(IEnumerable<User> users)
        {
            var dbSetMock = users.AsQueryable().BuildMockDbSet(); // async-capable IQueryable
            var repo = new Mock<IUserRepository>();
            repo.SetupGet(r => r.Queryable).Returns(dbSetMock.Object);
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            return repo;
        }

        public static void StubSetRolesNoop(this Mock<IUserRepository> repo)
            => repo.Setup(r => r.SetRolesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        /// <summary>
        /// Stubs AddAsync to echo the entity back, ensures navs initialized, and optionally invokes <paramref name="onAdded"/>.
        /// </summary>
        public static void StubAddEchoWithNavs(this Mock<IUserRepository> repo, Action<User>? onAdded = null)
        {
            repo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User u, CancellationToken _) =>
                {
                    if (u.Id == Guid.Empty) u.Id = Guid.NewGuid();
                    UserNavHelper.EnsureUserNavs(u, "User");
                    onAdded?.Invoke(u); // capture in caller if desired
                    return u;
                });
        }

        public static void StubUpdateEchoWithNavs(this Mock<IUserRepository> repo)
            => repo.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((User u, CancellationToken _) =>
                   {
                       UserNavHelper.EnsureUserNavs(u, "User");
                       return u;
                   });

        public static void StubReloadWithRoles(this Mock<IUserRepository> repo, Func<Guid, User> factory)
            => repo.Setup(r => r.GetWithRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Guid id, CancellationToken _) => factory(id));

        public static UserService NewService(this Mock<IUserRepository> repo) => new(repo.Object);
    }
}
