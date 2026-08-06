using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace JiApp.Common.Resilience;

public sealed class RetryPolicyFactory(TimeProvider timeProvider) : IRetryPolicyFactory
{
    public ResiliencePipeline RetryOnDbConflict(int retries, TimeSpan delay)
    {
        var builder = new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider
        };

        return builder
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = retries,
                Delay = delay,
                ShouldHandle = new PredicateBuilder().Handle<DbUpdateException>(),
                BackoffType = DelayBackoffType.Constant,
            })
            .Build();
    }

    public ResiliencePipeline RetryOnTransientHttp_WithExponentialBackoff(int retries = 3)
    {
        var builder = new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider
        };

        return builder
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = retries,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            })
            .Build();
    }
}
