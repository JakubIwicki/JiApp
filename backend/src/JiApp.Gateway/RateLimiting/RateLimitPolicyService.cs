using System.Collections.Concurrent;
using Microsoft.AspNetCore.RateLimiting;

namespace JiApp.Gateway.RateLimiting;

/// <summary>
/// Encapsulates endpoint manipulation for rate limit policy attachment.
/// Creates new endpoints with <see cref="EnableRateLimitingAttribute"/> metadata
/// appended, preserving the original endpoint's request delegate.
/// Results are cached by display name or path and policy name.
/// </summary>
public sealed class RateLimitPolicyService
{
    private static readonly ConcurrentDictionary<(string DisplayName, string PolicyName), Endpoint> EndpointCache =
        new();

    /// <summary>
    /// Creates a new endpoint with the given rate limit policy metadata appended,
    /// preserving the original endpoint's request delegate and display name.
    /// </summary>
    public Endpoint AttachRateLimitPolicy(Endpoint originalEndpoint, string policyName)
    {
        var cacheKey = (originalEndpoint.DisplayName ?? "", policyName);
        return EndpointCache.GetOrAdd(cacheKey, static (key, arg) =>
        {
            var (origEp, policy) = arg;
            var metadata = new EnableRateLimitingAttribute(policy);
            var newMetadata = origEp.Metadata.Append(metadata);
            return new Endpoint(
                origEp.RequestDelegate,
                new EndpointMetadataCollection(newMetadata),
                origEp.DisplayName);
        }, (originalEndpoint, policyName));
    }

    /// <summary>
    /// Creates a new endpoint with the given rate limit policy,
    /// using a no-op request delegate. Used when no original endpoint exists.
    /// </summary>
    public Endpoint CreatePolicyEndpoint(string path, string policyName)
    {
        var cacheKey = (path, policyName);
        return EndpointCache.GetOrAdd(cacheKey, static key =>
        {
            var metadata = new EnableRateLimitingAttribute(key.PolicyName);
            return new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(metadata),
                key.PolicyName);
        });
    }
}