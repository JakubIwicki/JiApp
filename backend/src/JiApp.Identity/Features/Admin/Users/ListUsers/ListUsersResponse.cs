namespace JiApp.Identity.Features.Admin.Users.ListUsers;

[Serializable]
public sealed record UserSummary(
	long Id,
	string? Username,
	string? Email,
	string? DisplayName,
	IReadOnlyList<string> Roles,
	bool IsLockedOut);

[Serializable]
public sealed record ListUsersResponse(
	IReadOnlyList<UserSummary> Users,
	int TotalCount);
