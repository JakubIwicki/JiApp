namespace JiApp.Identity.Features.Admin.Users.GetUserDetail;

[Serializable]
public sealed record GetUserDetailResponse(
	long Id,
	string? Username,
	string? Email,
	string? DisplayName,
	IReadOnlyList<string> Roles,
	bool IsLockedOut,
	DateTimeOffset? LockoutEnd);
