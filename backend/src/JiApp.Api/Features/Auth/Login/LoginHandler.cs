using System.Threading.Tasks;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Auth.Login;

public sealed class LoginHandler(
    SignInManager<User> signInManager,
    IJwtTokenService jwtTokenService,
    ILogger<LoginHandler> logger)
{
    public async Task<Result<LoginResponse>> HandleAsync(LoginRequest request)
    {
        logger.LoginAttempt(request.Username);

        var user = await signInManager.UserManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            logger.LoginFailedUserNotFound(request.Username);
            return Result<LoginResponse>.Failure("Invalid username or password");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            logger.LoginFailedAccountLocked(request.Username);
            return Result<LoginResponse>.Failure("Account is locked. Please try again later.");
        }

        if (!result.Succeeded)
        {
            logger.LoginFailedInvalidPassword(request.Username);
            return Result<LoginResponse>.Failure("Invalid username or password");
        }

        var token = jwtTokenService.GenerateToken(user.Id, user.UserName!);

        logger.LoginSuccessful(request.Username);
        return Result<LoginResponse>.Success(new LoginResponse(user.Id, user.DisplayName, token));
    }
}
