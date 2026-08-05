using System.Collections.Concurrent;
using System.Reflection;

namespace JiApp.Gateway.Tests.RateLimiting;

public sealed class RateLimitPolicyServiceTests
{
    private sealed class Fixture
    {
        public RateLimitPolicyService Sut { get; }

        private Fixture(int maxEntries) => Sut = new RateLimitPolicyService(maxEntries);

        public static Fixture Init(int maxEntries = 4096) => new(maxEntries);
    }

    [Fact]
    public void AttachRateLimitPolicy_AddsRateLimitingMetadata()
    {
        var fixture = Fixture.Init();
        var original = new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "test-endpoint-adds-rate-limiting-metadata");

        var result = fixture.Sut.AttachRateLimitPolicy(original, "TestPolicy");

        result.DisplayName.Should().Be("test-endpoint-adds-rate-limiting-metadata");
        result.Metadata.GetMetadata<EnableRateLimitingAttribute>()
            .Should().NotBeNull();
        result.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName
            .Should().Be("TestPolicy");
    }

    [Fact]
    public async Task AttachRateLimitPolicy_PreservesOriginalRequestDelegate()
    {
        var handlerInvoked = false;
        var fixture = Fixture.Init();
        var original = new Endpoint(
            _ =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            },
            EndpointMetadataCollection.Empty,
            "test-endpoint-preserves-delegate");

        var result = fixture.Sut.AttachRateLimitPolicy(original, "TestPolicy");

        await result.RequestDelegate!(new DefaultHttpContext());
        handlerInvoked.Should().BeTrue();
    }

    [Fact]
    public void CreatePolicyEndpoint_CreatesEndpoint_WithPolicyName()
    {
        var fixture = Fixture.Init();
        var result = fixture.Sut.CreatePolicyEndpoint("/test/path-policy-name", "TestPolicy");

        result.DisplayName.Should().Be("TestPolicy");
        result.Metadata.GetMetadata<EnableRateLimitingAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void AttachRateLimitPolicy_CachesEndpoint_ForSameParameters()
    {
        var fixture = Fixture.Init();
        var original = new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "test-endpoint-cache-same-params");

        var result1 = fixture.Sut.AttachRateLimitPolicy(original, "CachePolicy");
        var result2 = fixture.Sut.AttachRateLimitPolicy(original, "CachePolicy");

        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public void CreatePolicyEndpoint_CachesEndpoint_ForSamePathAndPolicy()
    {
        var fixture = Fixture.Init();
        var result1 = fixture.Sut.CreatePolicyEndpoint("/test/path-cache", "CachePolicy");
        var result2 = fixture.Sut.CreatePolicyEndpoint("/test/path-cache", "CachePolicy");

        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public void CreatePolicyEndpoint_DifferentPolicies_ProduceDifferentEndpoints()
    {
        var fixture = Fixture.Init();
        var result1 = fixture.Sut.CreatePolicyEndpoint("/test/path-diff-policy", "PolicyA");
        var result2 = fixture.Sut.CreatePolicyEndpoint("/test/path-diff-policy", "PolicyB");

        result1.Should().NotBeSameAs(result2);
        result1.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName.Should().Be("PolicyA");
        result2.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName.Should().Be("PolicyB");
    }

    [Fact]
    public void EndpointCache_NeverExceedsCap()
    {
        var sut = new RateLimitPolicyService(maxEntries: 2);

        sut.CreatePolicyEndpoint("/test/cap-1", "PolicyA");
        sut.CreatePolicyEndpoint("/test/cap-2", "PolicyA");
        sut.CreatePolicyEndpoint("/test/cap-3", "PolicyA");

        CacheCount(sut).Should().BeLessThanOrEqualTo(2);

        sut.AttachRateLimitPolicy(CreateEndpoint("/test/cap-4"), "PolicyA");
        sut.AttachRateLimitPolicy(CreateEndpoint("/test/cap-5"), "PolicyA");
        sut.AttachRateLimitPolicy(CreateEndpoint("/test/cap-6"), "PolicyA");

        CacheCount(sut).Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public void PreviouslyCachedKey_ResolvesAgain_AfterEviction()
    {
        var sut = new RateLimitPolicyService(maxEntries: 2);

        var original = sut.CreatePolicyEndpoint("/test/evict-1", "PolicyA");
        sut.CreatePolicyEndpoint("/test/evict-2", "PolicyA");
        sut.CreatePolicyEndpoint("/test/evict-3", "PolicyA");

        var recreated = sut.CreatePolicyEndpoint("/test/evict-1", "PolicyA");

        recreated.Should().NotBeSameAs(original);
        recreated.Metadata.GetMetadata<EnableRateLimitingAttribute>()
            .Should().NotBeNull();
        recreated.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName
            .Should().Be("PolicyA");
    }

    private static int CacheCount(RateLimitPolicyService sut)
    {
        var field = typeof(RateLimitPolicyService).GetField("_endpointCache",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RateLimitPolicyService._endpointCache field not found.");
        var cache = (ConcurrentDictionary<(string DisplayName, string PolicyName), Endpoint>)field.GetValue(sut)!;
        return cache.Count;
    }

    private static Endpoint CreateEndpoint(string path) => new(
        _ => Task.CompletedTask,
        new EndpointMetadataCollection(new EnableRateLimitingAttribute("PolicyA")),
        path);
}
