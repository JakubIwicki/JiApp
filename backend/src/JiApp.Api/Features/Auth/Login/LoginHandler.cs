using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Api.Features.Auth.Login;

public sealed class LoginHandler(
    UserManager<User> userManager,
    IJwtTokenService jwtTokenService)
{
    public async Task<Result<LoginResponse>> HandleAsync(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
            return Result<LoginResponse>.Failure("Invalid username or password");

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return Result<LoginResponse>.Failure("Invalid username or password");

        var token = jwtTokenService.GenerateToken(user.Id, user.UserName!);

        return Result<LoginResponse>.Success(new LoginResponse(user.Id, user.DisplayName, token));
    }
}
