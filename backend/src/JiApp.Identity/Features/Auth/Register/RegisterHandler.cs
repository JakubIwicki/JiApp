using System.Linq;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Logging;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace JiApp.Identity.Features.Auth.Register;

public sealed class RegisterHandler(
    UserManager<User> userManager,
    IUserAccessService accessService,
    ILogger<RegisterHandler> logger)
{
    public async Task<Result<RegisterResponse>> HandleAsync(RegisterRequest request, CancellationToken ct)
    {
        logger.ProcessingRegistration(request.Username);

        // Note: Pre-checks for duplicate username/email are intentionally omitted.
        // They create a TOCTOU race condition and leak user enumeration info.
        // Unique constraints are enforced at the DB level via indexes.
        // DbUpdateException is caught below for race conditions.
        // Password quality failures are returned as IdentityResult errors.

        var user = new User
        {
            UserName = request.Username,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        IdentityResult createResult;
        try
        {
            createResult = await userManager.CreateAsync(user, request.Password);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.RegistrationFailed(request.Username, "DB unique constraint violation");
            return Result<RegisterResponse>.Failure("Registration failed");
        }

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            logger.RegistrationFailed(request.Username, errors);
            return Result<RegisterResponse>.Failure(errors);
        }

        try
        {
            await accessService.AssignDefaultRoleAsync(user.Id);
        }
        catch (Exception ex)
        {
            logger.DefaultRoleAssignmentFailed(user.Id, ex);
            // Compensate: delete the user so registration stays atomic.
            // The caller sees a failure and can retry; no orphaned user
            // exists without a default role assigned.
            await userManager.DeleteAsync(user);
            return Result<RegisterResponse>.Failure("Registration failed");
        }

        logger.RegistrationCompleted(request.Username);
        return Result<RegisterResponse>.Success(new RegisterResponse(user.Id));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException switch
        {
            SqliteException sqliteEx => sqliteEx.SqliteErrorCode == 19, // SQLITE_CONSTRAINT
            PostgresException postgresEx => postgresEx.SqlState == "23505", // unique_violation
            _ => false,
        };
    }
}
