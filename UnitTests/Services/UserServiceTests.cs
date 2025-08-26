using Common.DTOs.Paging;
using Entities.Entites;      // User
using FluentAssertions;
using Moq;
// helper aliases
using UnitTests.Helpers;

namespace UnitTests.Services
{
    public class UserServiceTests
    {
        [Fact]
        public async Task ListPagedAsync_returns_expected_page_and_total()
        {
            // Arrange: make 23 users with safe (non-null) navs
            var users = Enumerable.Range(0, 23).Select(i =>
            {
                var u = new User
                {
                    Id = System.Guid.NewGuid(),
                    FirstName = $"First{i:D2}",
                    LastName = "X",
                    Email = $"u{i}@ex.com",
                    Active = true
                };
                UserNavHelper.EnsureUserNavs(u);
                return u;
            }).ToList();

            var repo = UserRepositoryStubs.MockRepoWithQueryable(users);
            var svc = repo.NewService();

            var req = new PagingRequest { Page = 2, PageSize = 5, Search = null, Sort = "FirstName" };

            // Act
            var result = await svc.ListPagedAsync(req, CancellationToken.None);

            // Assert
            result.TotalCount().Should().Be(23);
            result.Items().Count().Should().Be(5);
        }

        [Fact]
        public async Task CreateAsync_calls_AddAsync_and_returns_created()
        {
            // Arrange
            var repo = new Mock<Data.Repositories.IUserRepository>();
            repo.StubSetRolesNoop();

            User? added = null;                                 // capture entity passed to AddAsync
            repo.StubAddEchoWithNavs(u => added = u);          // callback instead of 'out' param

            // If your service reloads with includes post-create, stub that too
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
                UserNavHelper.EnsureUserNavs(u, "User");
                return u;
            });

            var svc = repo.NewService();
            var dto = UserDtoFactory.NewCreateDto();

            // Act
            var created = await svc.CreateAsync(dto, CancellationToken.None);

            // Assert
            repo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
            created.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_returns_null_when_user_not_found()
        {
            // Arrange
            var repo = new Mock<Data.Repositories.IUserRepository>();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<System.Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var svc = repo.NewService();
            var dto = UserDtoFactory.NewUpdateDto("John", "Smith", "john@example.com");

            // Act
            var result = await svc.UpdateAsync(System.Guid.NewGuid(), dto, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            repo.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_updates_and_returns_value_when_found()
        {
            // Arrange
            var existing = new User
            {
                Id = System.Guid.NewGuid(),
                FirstName = "Old",
                LastName = "Name",
                Email = "old@example.com",
                Active = true
            };
            UserNavHelper.EnsureUserNavs(existing, "User");

            var repo = new Mock<Data.Repositories.IUserRepository>();
            repo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<System.Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            repo.StubSetRolesNoop();
            repo.StubUpdateEchoWithNavs();

            // many services reload with includes after update for mapping
            repo.Setup(r => r.GetWithRolesAsync(existing.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((System.Guid id, CancellationToken _) =>
                {
                    var u = new User
                    {
                        Id = id,
                        FirstName = "New",
                        LastName = "Name",
                        Email = "new@example.com",
                        Active = true
                    };
                    UserNavHelper.EnsureUserNavs(u, "User");
                    return u;
                });

            var svc = repo.NewService();
            var dto = UserDtoFactory.NewUpdateDto();

            // Act
            var result = await svc.UpdateAsync(existing.Id, dto, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            repo.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_returns_true_when_deleted_and_false_when_missing()
        {
            // Arrange
            var existingId = System.Guid.NewGuid();

            var repo = new Mock<Data.Repositories.IUserRepository>();
            repo.Setup(r => r.DeleteAsync(existingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            repo.Setup(r => r.DeleteAsync(It.Is<System.Guid>(g => g != existingId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var svc = repo.NewService();

            // Act + Assert
            (await svc.DeleteAsync(existingId, CancellationToken.None)).Should().BeTrue();
            (await svc.DeleteAsync(System.Guid.NewGuid(), CancellationToken.None)).Should().BeFalse();
        }
    }
}
