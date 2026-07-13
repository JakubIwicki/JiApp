using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Users.GetUserDetail;
using JiApp.Identity.Tests.Mocks;

namespace JiApp.Identity.Tests.Features.Admin.Users.GetUserDetail;

public sealed class GetUserDetailHandlerTests
{
	private sealed class Fixture
	{
		private readonly User _testUser = new()
		{
			Id = 1,
			UserName = "testuser",
			Email = "test@test.com",
			DisplayName = "Test User"
		};

		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();

		public GetUserDetailHandler Sut { get; }

		public Fixture()
		{
			Sut = new GetUserDetailHandler(UserManagerDouble.Object);
		}

		public Fixture WithExistingUser()
		{
			UserManagerDouble.WithFindByIdAsync("1", _testUser);
			UserManagerDouble.WithGetRolesAsync(_testUser, ["User"]);
			return this;
		}

		public Fixture WithNonexistentUser()
		{
			UserManagerDouble.WithFindByIdAsync("999", null);
			return this;
		}

		public Fixture WithLockedOutUser()
		{
			_testUser.LockoutEnd = DateTimeOffset.MaxValue;
			return WithExistingUser();
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsUserDetail_ForExistingUser()
	{
		var fixture = new Fixture().WithExistingUser();

		var result = await fixture.Sut.HandleAsync(1, CancellationToken.None);

		AssertSuccess(result);
		result.Value!.Id.Should().Be(1);
		result.Value.Username.Should().Be("testuser");
		result.Value.IsLockedOut.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_ForNonexistentUser()
	{
		var fixture = new Fixture().WithNonexistentUser();

		var result = await fixture.Sut.HandleAsync(999, CancellationToken.None);

		AssertNotFound(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsIsLockedOutTrue_WhenLockedOut()
	{
		var fixture = new Fixture().WithLockedOutUser();

		var result = await fixture.Sut.HandleAsync(1, CancellationToken.None);

		AssertSuccess(result);
		result.Value!.IsLockedOut.Should().BeTrue();
		result.Value.LockoutEnd.Should().Be(DateTimeOffset.MaxValue);
	}
}
