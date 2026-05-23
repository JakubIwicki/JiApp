using System.Linq;
using System.Threading.Tasks;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Auth.Register;

public sealed class RegisterHandler(
    UserManager<User> userManager,
    ILogger<RegisterHandler> logger)
{
    public async Task<Result<RegisterResponse>> HandleAsync(RegisterRequest request)
    {
        logger.ProcessingRegistration(request.Username);

        var existingUser = await userManager.FindByNameAsync(request.Username);
        if (existingUser is not null)
        {
            logger.RegistrationFailedUsernameTaken(request.Username);
            return Result<RegisterResponse>.Failure("Username already taken");
        }

        var existingEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingEmail is not null)
        {
            logger.RegistrationFailedEmailTaken(request.Email);
            return Result<RegisterResponse>.Failure("Email already taken");
        }

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
            logger.RegistrationFailedWithErrors(request.Username, errors);
            return Result<RegisterResponse>.Failure(errors);
        }

        logger.RegistrationCompleted(request.Username);
        return Result<RegisterResponse>.Success(new RegisterResponse());
    }
}