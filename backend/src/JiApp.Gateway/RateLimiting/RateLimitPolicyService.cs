using System.Collections.Concurrent;
using Microsoft.AspNetCore.RateLimiting;

namespace JiApp.Gateway.RateLimiting;

/// <summary>
/// Encapsulates endpoint manipulation for rate limit policy attachment.
/// Creates new endpoints with <see cref="EnableRateLimitingAttribute"/> metadata
/// appended, preserving the original endpoint's request delegate.
/// Results are cached by display name or path and policy name. The cache is bounded
/// by the configured max entries; once the cap is reached it is cleared wholesale.
/// Entries are pure functions of immutable (endpoint/path, policy) metadata, so
/// eviction is safe — the next lookup simply rebuilds the endpoint.
/// </summary>
public sealed class RateLimitPolicyService
{
    private readonly ConcurrentDictionary<(string DisplayName, string PolicyName), Endpoint> _endpointCache = new();
    private readonly int _maxEntries;

    public RateLimitPolicyService(int maxEntries)
    {
        _maxEntries = maxEntries;
    }

    /// <summary>
    /// Creates a new endpoint with the given rate limit policy metadata appended,
    /// preserving the original endpoint's request delegate and display name.
    /// </summary>
    public Endpoint AttachRateLimitPolicy(Endpoint originalEndpoint, string policyName)
    {
        var cacheKey = (originalEndpoint.DisplayName ?? "", policyName);
        if (_endpointCache.Count >= _maxEntries)
            _endpointCache.Clear();
        return _endpointCache.GetOrAdd(cacheKey, static (key, arg) =>
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
        if (_endpointCache.Count >= _maxEntries)
            _endpointCache.Clear();
        return _endpointCache.GetOrAdd(cacheKey, static key =>
        {
            var metadata = new EnableRateLimitingAttribute(key.PolicyName);
            return new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(metadata),
                key.PolicyName);
        });
    }
}