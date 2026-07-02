namespace JiApp.Common.Services;

public enum StampValidationResult { Valid, Revoked, Unavailable }

public interface ISecurityStampValidator
{
	Task<StampValidationResult> ValidateCurrentAsync(CancellationToken ct = default);
}
