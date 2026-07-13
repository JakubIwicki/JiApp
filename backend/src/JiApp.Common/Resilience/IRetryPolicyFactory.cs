using Polly;

namespace JiApp.Common.Resilience;

public interface IRetryPolicyFactory
{
	ResiliencePipeline RetryOnDbConflict(int retries, TimeSpan delay);
	ResiliencePipeline RetryOnTransientHttp_WithExponentialBackoff(int retries = 3);
}
