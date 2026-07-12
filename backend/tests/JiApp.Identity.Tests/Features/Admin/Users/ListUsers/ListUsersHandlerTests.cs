using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Users.ListUsers;
using JiApp.Identity.Tests.Mocks;
using JiApp.Testing.Common.Data;

namespace JiApp.Identity.Tests.Features.Admin.Users.ListUsers;

public sealed class ListUsersHandlerTests
{
	private sealed class Fixture
	{
		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();

		public ListUsersHandler Sut { get; }

		private readonly List<User> _users = [];

		public Fixture()
		{
			Sut = new ListUsersHandler(UserManagerDouble.Object);
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

			var queryable = _users.AsAsyncQueryable();
			UserManagerDouble.WithUsersQueryable(queryable);
			UserManagerDouble.WithGetRolesAsyncForAny(["User"]);
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

		var result = await fixture.Sut.HandleAsync(null, 1, 20, CancellationToken.None);

		AssertSuccess(result);
		result.Value!.Users.Should().HaveCount(5);
		result.Value.TotalCount.Should().Be(5);
	}

	[Fact]
	public async Task HandleAsync_ReturnsPaginatedResults()
	{
		var fixture = new Fixture().WithUsers(5);

		var result = await fixture.Sut.HandleAsync(null, 1, 2, CancellationToken.None);

		AssertSuccess(result);
		result.Value!.Users.Should().HaveCount(2);
		result.Value.TotalCount.Should().Be(5);
	}

	[Fact]
	public async Task HandleAsync_ReturnsIsLockedOutTrue_WhenUserIsLockedOut()
	{
		var fixture = new Fixture().WithUsers(1).WithUserLockedOut(1);

		var result = await fixture.Sut.HandleAsync(null, 1, 20, CancellationToken.None);

		AssertSuccess(result);
		result.Value!.Users.Single().IsLockedOut.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_ReturnsCorrectPage_WhenMoreUsersThanPageSize()
	{
		var fixture = new Fixture().WithUsers(10);

		var result = await fixture.Sut.HandleAsync(null, 2, 3, CancellationToken.None);

		AssertSuccess(result);
		result.Value!.Users.Should().HaveCount(3);
		result.Value.TotalCount.Should().Be(10);
		result.Value.Users[0].Id.Should().Be(4);
	}
}
