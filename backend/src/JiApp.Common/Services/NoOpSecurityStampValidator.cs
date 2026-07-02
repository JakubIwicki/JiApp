namespace JiApp.Common.Services;

/// <summary>
/// Development-only fallback that always returns Valid. Used when no IdentityBaseUrl
/// is configured in a Development environment.
/// </summary>
public sealed class NoOpSecurityStampValidator : ISecurityStampValidator
{
	public Task<StampValidationResult> ValidateCurrentAsync(CancellationToken ct = default)
		=> Task.FromResult(StampValidationResult.Valid);
}
