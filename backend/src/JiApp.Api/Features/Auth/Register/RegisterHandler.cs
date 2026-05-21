using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Api.Features.Auth.Register;

public sealed class RegisterHandler(UserManager<User> userManager)
{
    public async Task<Result<RegisterResponse>> HandleAsync(RegisterRequest request)
    {
        var existingUser = await userManager.FindByNameAsync(request.Username);
        if (existingUser is not null)
            return Result<RegisterResponse>.Failure("Username already taken");

        var existingEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingEmail is not null)
            return Result<RegisterResponse>.Failure("Email already taken");

        var user = new User
        {
            UserName = request.Username,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result<RegisterResponse>.Failure(errors);
        }

        return Result<RegisterResponse>.Success(new RegisterResponse());
    }
}
