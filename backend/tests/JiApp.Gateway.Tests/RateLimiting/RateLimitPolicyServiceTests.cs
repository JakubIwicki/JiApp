namespace JiApp.Gateway.Tests.RateLimiting;

public class RateLimitPolicyServiceTests
{
    private readonly RateLimitPolicyService _service = new();

    [Fact]
    public void AttachRateLimitPolicy_adds_rate_limiting_metadata()
    {
        var original = new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "test-endpoint-adds-rate-limiting-metadata");

        var result = _service.AttachRateLimitPolicy(original, "TestPolicy");

        result.DisplayName.Should().Be("test-endpoint-adds-rate-limiting-metadata");
        result.Metadata.GetMetadata<EnableRateLimitingAttribute>()
            .Should().NotBeNull();
        result.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName
            .Should().Be("TestPolicy");
    }

    [Fact]
    public async Task AttachRateLimitPolicy_preserves_original_request_delegate()
    {
        var handlerInvoked = false;
        var original = new Endpoint(
            _ =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            },
            EndpointMetadataCollection.Empty,
            "test-endpoint-preserves-delegate");

        var result = _service.AttachRateLimitPolicy(original, "TestPolicy");

        // Invoke the returned endpoint's request delegate to verify it works
        await result.RequestDelegate(new DefaultHttpContext());
        handlerInvoked.Should().BeTrue();
    }

    [Fact]
    public void CreatePolicyEndpoint_creates_endpoint_with_policy_name()
    {
        var result = _service.CreatePolicyEndpoint("/test/path-policy-name", "TestPolicy");

        result.DisplayName.Should().Be("TestPolicy");
        result.Metadata.GetMetadata<EnableRateLimitingAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void AttachRateLimitPolicy_caches_endpoint_for_same_parameters()
    {
        var original = new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "test-endpoint-cache-same-params");

        var result1 = _service.AttachRateLimitPolicy(original, "CachePolicy");
        var result2 = _service.AttachRateLimitPolicy(original, "CachePolicy");

        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public void CreatePolicyEndpoint_caches_endpoint_for_same_path_and_policy()
    {
        var result1 = _service.CreatePolicyEndpoint("/test/path-cache", "CachePolicy");
        var result2 = _service.CreatePolicyEndpoint("/test/path-cache", "CachePolicy");

        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public void CreatePolicyEndpoint_different_policies_produce_different_endpoints()
    {
        var result1 = _service.CreatePolicyEndpoint("/test/path-diff-policy", "PolicyA");
        var result2 = _service.CreatePolicyEndpoint("/test/path-diff-policy", "PolicyB");

        result1.Should().NotBeSameAs(result2);
        result1.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName.Should().Be("PolicyA");
        result2.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName.Should().Be("PolicyB");
    }
}
