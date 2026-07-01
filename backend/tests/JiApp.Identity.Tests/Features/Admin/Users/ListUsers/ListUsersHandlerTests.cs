using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Users.ListUsers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Users.ListUsers;

public sealed class ListUsersHandlerTests
{
	private sealed class Fixture
	{
		public Mock<UserManager<User>> UserManagerMock { get; } = new(
			Mock.Of<IUserStore<User>>(),
			Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
			Mock.Of<IPasswordHasher<User>>(),
			Array.Empty<IUserValidator<User>>(),
			Array.Empty<IPasswordValidator<User>>(),
			Mock.Of<ILookupNormalizer>(),
			Mock.Of<IdentityErrorDescriber>(),
			Mock.Of<IServiceProvider>(),
			Mock.Of<ILogger<UserManager<User>>>());

		public ListUsersHandler Sut { get; }

		private readonly List<User> _users = [];

		public Fixture()
		{
			Sut = new ListUsersHandler(UserManagerMock.Object);
		}

		public Fixture WithUsers(int count)
		{
			_users.Clear();
			for (var i = 1; i <= count; i++)
			{
				_users.Add(new User
				{
					Id = i,
					UserName = $"user{i}",
					Email = $"user{i}@test.com",
					DisplayName = $"User {i}"
				});
			}

			var queryable = _users.AsQueryable();
			UserManagerMock.Setup(x => x.Users).Returns(queryable);
			UserManagerMock
				.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
				.ReturnsAsync(["User"]);
			return this;
		}

		public Fixture WithUserLockedOut(long userId)
		{
			var user = _users.First(u => u.Id == userId);
			user.LockoutEnd = DateTimeOffset.MaxValue;
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsAllUsers_WhenNoSearchProvided()
	{
		var fixture = new Fixture().WithUsers(5);

		var result = await fixture.Sut.HandleAsync(null, 1, 20);

		AssertSuccess(result);
		result.Value!.Users.Should().HaveCount(5);
		result.Value.TotalCount.Should().Be(5);
	}

	[Fact]
	public async Task HandleAsync_ReturnsPaginatedResults()
	{
		var fixture = new Fixture().WithUsers(5);

		var result = await fixture.Sut.HandleAsync(null, 1, 2);

		AssertSuccess(result);
		result.Value!.Users.Should().HaveCount(2);
		result.Value.TotalCount.Should().Be(5);
	}

	[Fact]
	public async Task HandleAsync_ReturnsIsLockedOutTrue_WhenUserIsLockedOut()
	{
		var fixture = new Fixture().WithUsers(1).WithUserLockedOut(1);

		var result = await fixture.Sut.HandleAsync(null, 1, 20);

		AssertSuccess(result);
		result.Value!.Users.Single().IsLockedOut.Should().BeTrue();
	}
}
