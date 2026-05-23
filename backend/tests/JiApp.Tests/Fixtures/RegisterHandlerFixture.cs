using JiApp.Api.Features.Auth.Register;
using JiApp.Common.Models;
using JiApp.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class RegisterHandlerFixture
{
    private readonly Mock<UserManager<User>> _userManagerMock;

    public RegisterHandlerFixture()
    {
        _userManagerMock = UserManagerMock.GetSuccessful();
    }

    public RegisterHandlerFixture WithFindByNameAsync(string username, User? user)
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(username)).ReturnsAsync(user);
        return this;
    }

    public RegisterHandlerFixture WithAnyFindByNameAsync(User? user)
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
        return this;
    }

    public RegisterHandlerFixture WithFindByEmailAsync(string email, User? user)
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        return this;
    }

    public RegisterHandlerFixture WithAnyFindByEmailAsync(User? user)
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        return this;
    }

    public RegisterHandlerFixture WithAnyCreateAsync(IdentityResult result)
    {
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(result);
        return this;
    }

    public RegisterHandlerContext Build()
    {
        var handler = new RegisterHandler(
            _userManagerMock.Object,
            LoggerMock.Of<RegisterHandler>());

        return new RegisterHandlerContext(handler, _userManagerMock);
    }
}

public sealed record RegisterHandlerContext(
    RegisterHandler Handler,
    Mock<UserManager<User>> UserManagerMock);
