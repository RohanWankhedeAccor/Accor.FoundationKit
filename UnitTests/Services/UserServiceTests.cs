using Business.Services;
using Common.DTOs.Paging;
using Data.Repositories;
using Entities.Entites;
using FluentAssertions;
using MockQueryable.Moq;          // IMPORTANT
using Moq;

namespace UnitTests.Services;
public class UserServiceTests
{
    [Fact]
    public async Task ListPagedAsync_returns_expected_page()
    {
        // Arrange
        var users = Enumerable.Range(0, 25).Select(i => new User
        {
            Id = Guid.NewGuid(),
            FirstName = $"First{i:D2}",
            LastName = $"Last{i:D2}",
            Email = $"u{i}@ex.com",
            Active = true
        }).ToList();

        // Build async-capable DbSet<User> mock
        var dbSetMock = users.AsQueryable().BuildMockDbSet();   // <- Mock<DbSet<User>>

        var repo = new Mock<IUserRepository>();
        // DbSet<T> implements IQueryable<T>, so this satisfies your Queryable property
        repo.SetupGet(r => r.Queryable).Returns(dbSetMock.Object);

        // stub any extra methods your service calls
        repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = new UserService(repo.Object);

        var req = new PagingRequest { Page = 2, PageSize = 5, Search = null, Sort = "FirstName" };

        // Act
        var result = await svc.ListPagedAsync(req, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var total = (int)result.GetType().GetProperty("TotalCount")!.GetValue(result)!;
        total.Should().Be(25);
    }
}
