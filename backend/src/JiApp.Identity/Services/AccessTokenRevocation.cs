using JiApp.Common.Models;

namespace JiApp.Identity.Services;

public static class AccessTokenRevocation
{
	public static bool IsValid(User? user, string? tokenSecurityStamp)
		=> user is not null && tokenSecurityStamp is not null && user.SecurityStamp == tokenSecurityStamp;
}
