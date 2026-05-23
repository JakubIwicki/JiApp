using JiApp.Api.Features.Auth.Me;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

internal sealed class MeHandlerFixture
{
    private readonly Mock<UserManager<User>> _userManagerMock = UserManagerMock.GetSuccessful();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = CurrentUserServiceMock.GetSuccessful();

    public MeHandlerFixture WithUserId(long userId)
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        return this;
    }

    public MeHandlerFixture WithFindByIdAsync(string userIdString, User? user)
    {
        _userManagerMock.Setup(x => x.FindByIdAsync(userIdString)).ReturnsAsync(user);
        return this;
    }

    public MeHandlerContext Build()
    {
        var handler = new MeHandler(
            _userManagerMock.Object,
            _currentUserServiceMock.Object,
            LoggerMock.Of<MeHandler>());

        return new MeHandlerContext(handler);
    }
}

internal sealed record MeHandlerContext(
    MeHandler Handler);