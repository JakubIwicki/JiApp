using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Testing.Common.Conventions;

/// <summary>
/// Shared convention: every mapped endpoint must carry authorization metadata
/// (<see cref="IAuthorizeData"/> or equivalent) unless its route pattern is on
/// an explicit allow-list. Collects all violations so a single assertion reports
/// every offender.
/// </summary>
public static class EndpointAuthorizationConvention
{
	/// <summary>
	/// Enumerate all <see cref="RouteEndpoint"/> entries from the given data sources
	/// and collect violations where an endpoint lacks authorization metadata and is
	/// not on the allow-list.
	/// </summary>
	/// <param name="dataSources">Resolved from DI after the host is started.</param>
	/// <param name="allowList">
	/// Route patterns (without leading slash; matched case-insensitively)
	/// that are intentionally anonymous. Every entry must carry a comment in the
	/// test file explaining why.
	/// </param>
	/// <returns>One violation string per offender, empty if no violations.</returns>
	public static List<string> CollectUnauthorizedEndpoints(
		IEnumerable<EndpointDataSource> dataSources,
		HashSet<string> allowList)
	{
		var violations = new List<string>();
		var processedPatterns = new HashSet<string>(StringComparer.Ordinal);

		foreach (var ds in dataSources)
		{
			foreach (var endpoint in ds.Endpoints)
			{
				if (endpoint is not RouteEndpoint routeEndpoint)
					continue;

				var pattern = routeEndpoint.RoutePattern.RawText;
				if (pattern is null)
					continue;

				// Deduplicate: multiple data sources may expose the same endpoint.
				if (!processedPatterns.Add(pattern))
					continue;

				var normalized = pattern.TrimStart('/');

				if (allowList.Contains(normalized))
					continue;

				var hasAllowAnonymous = endpoint.Metadata
					.Any(m => m is IAllowAnonymous);
				var hasAuth = endpoint.Metadata
					.Any(m => m is IAuthorizeData);

				if (hasAllowAnonymous)
				{
					violations.Add(
						$"  {pattern} — has [AllowAnonymous] but is NOT on the allow-list. " +
						"Add it to the allow-list with a comment or require authorization.");
				}
				else if (!hasAuth)
				{
					violations.Add(
						$"  {pattern} — no authorization metadata found. " +
						"Add .RequireAuthorization() or .AllowAnonymous() with an allow-list entry.");
				}
				// else: authorized — OK
			}
		}

		return violations;
	}
}
