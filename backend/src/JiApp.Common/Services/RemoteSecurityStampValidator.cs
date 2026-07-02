using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JiApp.Common.Services;

public sealed class RemoteSecurityStampValidator(
	HttpClient httpClient,
	IHttpContextAccessor httpContextAccessor,
	ILogger<RemoteSecurityStampValidator> logger) : ISecurityStampValidator
{
	public async Task<StampValidationResult> ValidateCurrentAsync(CancellationToken ct = default)
	{
		var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization;
		if (authHeader is not { Count: > 0 } header || string.IsNullOrEmpty(header[0]))
		{
			logger.LogWarning("Security-stamp recheck: no Authorization header present on the current request");
			return StampValidationResult.Unavailable;
		}

		if (!header[0]!.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
		{
			logger.LogWarning("Security-stamp recheck: Authorization header is not a Bearer token");
			return StampValidationResult.Unavailable;
		}

		var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/validate");
		request.Headers.TryAddWithoutValidation("Authorization", header[0]);

		try
		{
			using var response = await httpClient.SendAsync(request, ct);

			if (response.IsSuccessStatusCode)
				return StampValidationResult.Valid;

			if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				return StampValidationResult.Revoked;

			logger.LogWarning(
				"Security-stamp recheck: Identity returned unexpected status {StatusCode}",
				(int)response.StatusCode);
			return StampValidationResult.Unavailable;
		}
		catch (HttpRequestException ex)
		{
			logger.LogWarning(ex, "Security-stamp recheck: HTTP call to Identity failed");
			return StampValidationResult.Unavailable;
		}
		catch (TaskCanceledException) when (!ct.IsCancellationRequested)
		{
			logger.LogWarning("Security-stamp recheck: HTTP call to Identity timed out");
			return StampValidationResult.Unavailable;
		}
	}
}
