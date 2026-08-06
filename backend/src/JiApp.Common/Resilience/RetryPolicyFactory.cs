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
                // A caller-initiated cancellation must never be retried, for either
                // exception family — the decision keys on the caller's token, never the
                // exception's (a genuine HttpClient timeout still retries). Deliberate:
                // plain OperationCanceledException is retried too when the caller is not cancelled.
                ShouldHandle = args => new ValueTask<bool>(
                    (args.Outcome.Exception is HttpRequestException or OperationCanceledException)
                    && !args.Context.CancellationToken.IsCancellationRequested),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            })
            .Build();
    }
}
