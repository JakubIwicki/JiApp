using System.Linq;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace JiApp.Identity.Features.Admin.Users.CreateUser;

public sealed class CreateUserHandler(
	UserManager<User> userManager,
	RoleManager<IdentityRole<long>> roleManager)
{
	public async Task<Result<CreateUserResponse>> HandleAsync(CreateUserRequest request)
	{
		foreach (var roleName in request.Roles)
		{
			if (!await roleManager.RoleExistsAsync(roleName))
				return Result<CreateUserResponse>.Failure(
					$"Role '{roleName}' does not exist", ResultCategories.Validation);
		}

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
			return Result<CreateUserResponse>.Failure("Registration failed", ResultCategories.Conflict);
		}

		if (!createResult.Succeeded)
		{
			var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
			return Result<CreateUserResponse>.Failure(errors, ResultCategories.Validation);
		}

		if (request.Roles.Length > 0)
		{
			var roleResult = await userManager.AddToRolesAsync(user, request.Roles);
			if (!roleResult.Succeeded)
			{
				await userManager.DeleteAsync(user);
				var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
				return Result<CreateUserResponse>.Failure(errors, ResultCategories.Validation);
			}
		}

		return Result<CreateUserResponse>.Success(new CreateUserResponse(user.Id));
	}

	private static bool IsUniqueConstraintViolation(DbUpdateException ex)
	{
		return ex.InnerException switch
		{
			SqliteException sqliteEx => sqliteEx.SqliteErrorCode == 19,
			PostgresException postgresEx => postgresEx.SqlState == "23505",
			_ => false,
		};
	}
}
