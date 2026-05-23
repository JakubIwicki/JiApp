using System;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JiApp.Tests.Mocks;

public static class UserManagerMock
{
    public static Mock<UserManager<User>> GetSuccessful()
    {
        var store = Mock.Of<IUserStore<User>>();
        var options = Mock.Of<IOptions<IdentityOptions>>();
        var hasher = Mock.Of<IPasswordHasher<User>>();
        var normalizer = Mock.Of<ILookupNormalizer>();
        var describer = Mock.Of<IdentityErrorDescriber>();
        var services = Mock.Of<IServiceProvider>();
        var logger = Mock.Of<ILogger<UserManager<User>>>();

        return new Mock<UserManager<User>>(
            store, options, hasher,
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            normalizer, describer, services, logger);
    }
}

public static class SignInManagerMock
{
    public static Mock<SignInManager<User>> GetSuccessful(Mock<UserManager<User>> userManagerMock)
    {
        var contextAccessor = Mock.Of<IHttpContextAccessor>();
        var claimsFactory = Mock.Of<IUserClaimsPrincipalFactory<User>>();
        var options = Mock.Of<IOptions<IdentityOptions>>();
        var logger = Mock.Of<ILogger<SignInManager<User>>>();
        var schemeProvider = Mock.Of<IAuthenticationSchemeProvider>();
        var confirmation = Mock.Of<IUserConfirmation<User>>();

        return new Mock<SignInManager<User>>(
            userManagerMock.Object,
            contextAccessor,
            claimsFactory,
            options,
            logger,
            schemeProvider,
            confirmation);
    }
}

public static class JwtTokenServiceMock
{
    public static Mock<IJwtTokenService> GetSuccessful()
        => new();
}