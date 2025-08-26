using Business.Services;        // RoleService
using Data.Repositories;        // IRoleRepository
using Entities.Entites;         // Role (entity)
using FluentAssertions;
using Moq;

namespace UnitTests.Services
{
    public class RoleServiceTests
    {
        [Fact]
        public async Task ListAsync_returns_all_roles_as_dtos()
        {
            // Arrange
            var seeded = new List<Role>
            {
                new Role { Id = Guid.NewGuid(), Name = "Admin" },
                new Role { Id = Guid.NewGuid(), Name = "Super Admin" },
                new Role { Id = Guid.NewGuid(), Name = "User" },
                new Role { Id = Guid.NewGuid(), Name = "Viewer" },
            };

            var repo = new Mock<IRoleRepository>();
            repo.Setup(r => r.ListRolesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(seeded);

            var svc = new RoleService(repo.Object);

            // Act
            var result = await svc.ListAsync(CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(seeded.Count);

            // Compare mapped properties (order-insensitive)
            result.Select(r => r.Id).Should().BeEquivalentTo(seeded.Select(s => s.Id));
            result.Select(r => r.Name).Should().BeEquivalentTo(seeded.Select(s => s.Name));

            repo.Verify(r => r.ListRolesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListAsync_returns_empty_when_no_roles_exist()
        {
            // Arrange
            var repo = new Mock<IRoleRepository>();
            repo.Setup(r => r.ListRolesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Role>());

            var svc = new RoleService(repo.Object);

            // Act
            var result = await svc.ListAsync(CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            repo.Verify(r => r.ListRolesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
