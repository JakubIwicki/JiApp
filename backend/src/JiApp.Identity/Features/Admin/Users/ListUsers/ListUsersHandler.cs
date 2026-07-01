using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Identity.Features.Admin.Users.ListUsers;

public sealed class ListUsersHandler(UserManager<User> userManager)
{
	public async Task<Result<ListUsersResponse>> HandleAsync(string? search, int page, int pageSize)
	{
		var query = userManager.Users.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			query = query.Where(u =>
				(u.UserName != null && u.UserName.Contains(term)) ||
				(u.Email != null && u.Email.Contains(term)));
		}

		var totalCount = await query.CountAsync();
		var pagedUsers = await query.OrderBy(u => u.Id)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		var summaries = new List<UserSummary>(pagedUsers.Count);
		foreach (var user in pagedUsers)
		{
			var roles = await userManager.GetRolesAsync(user);
			var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
			summaries.Add(new UserSummary(user.Id, user.UserName, user.Email, user.DisplayName, [.. roles], isLockedOut));
		}

		return Result<ListUsersResponse>.Success(new ListUsersResponse([.. summaries], totalCount));
	}
}
