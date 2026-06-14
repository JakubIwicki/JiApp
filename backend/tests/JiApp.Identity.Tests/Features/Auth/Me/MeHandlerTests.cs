using System.Globalization;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.Me;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Me;

public class MeHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IUserModuleGrantService> _grantServiceMock;
    private readonly User _testUser;
    private readonly MeHandler _sut;

    public MeHandlerTests()
    {
        _testUser = new User
        {
            Id = 1,
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            SecurityStamp = "stamp",
            ConcurrencyStamp = "concurrency"
        };

        _userManagerMock = CreateUserManagerMock();
        _currentUserMock = new Mock<ICurrentUserService>();
        _grantServiceMock = new Mock<IUserModuleGrantService>();
        var logger = Mock.Of<ILogger<MeHandler>>();

        _sut = new MeHandler(
            _userManagerMock.Object, _currentUserMock.Object, _grantServiceMock.Object, logger);
    }

    [Fact]
    public async Task HandleAsync_returns_profile_for_valid_user()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(1);
        _currentUserMock.Setup(x => x.Username).Returns("testuser");
        _userManagerMock.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(_testUser);
        _grantServiceMock.Setup(x => x.GetModulesAsync(1))
            .ReturnsAsync(["YtDownloader", "Scheduler"]);

        var result = await _sut.HandleAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(_testUser.Id);
        result.Value.DisplayName.Should().Be(_testUser.DisplayName);
        result.Value.Username.Should().Be("testuser");
        result.Value.Modules.Should().BeEquivalentTo("YtDownloader", "Scheduler");
    }

    [Fact]
    public async Task HandleAsync_returns_failure_for_nonexistent_user()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(999);
        _userManagerMock.Setup(x => x.FindByIdAsync("999"))
            .ReturnsAsync((User?)null);

        var result = await _sut.HandleAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        return new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(),
            Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());
    }
}