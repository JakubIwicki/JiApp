using JiApp.Api.Features.Auth.Login;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class LoginHandlerFixture
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;

    public LoginHandlerFixture()
    {
        _userManagerMock = UserManagerMock.GetSuccessful();
        _signInManagerMock = SignInManagerMock.GetSuccessful(_userManagerMock);
        _jwtTokenServiceMock = JwtTokenServiceMock.GetSuccessful();
    }

    public LoginHandlerFixture WithFindByNameAsync(string username, User? user)
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(username)).ReturnsAsync(user);
        return this;
    }

    public LoginHandlerFixture WithAnyFindByNameAsync(User? user)
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
        return this;
    }

    public LoginHandlerFixture WithCheckPasswordSignInAsync(User user, string password, SignInResult result)
    {
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, true)).ReturnsAsync(result);
        return this;
    }

    public LoginHandlerFixture WithGenerateToken(long userId, string username, string token)
    {
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(userId, username)).Returns(token);
        return this;
    }

    public LoginHandlerContext Build()
    {
        var handler = new LoginHandler(
            _signInManagerMock.Object,
            _jwtTokenServiceMock.Object,
            LoggerMock.Of<LoginHandler>());

        return new LoginHandlerContext(handler, _userManagerMock, _signInManagerMock, _jwtTokenServiceMock);
    }
}

public sealed record LoginHandlerContext(
    LoginHandler Handler,
    Mock<UserManager<User>> UserManagerMock,
    Mock<SignInManager<User>> SignInManagerMock,
    Mock<IJwtTokenService> JwtTokenServiceMock);
