namespace JiApp.Gateway.Tests.RateLimiting;

public sealed class RateLimitPolicySelectorTests
{
    private sealed class Fixture
    {
        public RateLimitPolicySelector Sut { get; }

        public Fixture(RequestDelegate? next = null)
        {
            Sut = new RateLimitPolicySelector(next ?? (_ => Task.CompletedTask), new RateLimitPolicyService());
        }

        public static DefaultHttpContext CreateContext(string path)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString(path);
            context.Request.Method = "GET";
            context.Response.Body = new MemoryStream();
            return context;
        }

        public static Fixture Init() => new();
    }

    [Fact]
    public async Task Matches_uppercase_path_with_case_insensitive_exact_match()
    {
        var context = Fixture.CreateContext("/API/V1/AUTH/LOGIN");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("LoginPolicy");
    }

    [Fact]
    public async Task Matches_login_path_to_LoginPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/auth/login");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("LoginPolicy");
    }

    [Fact]
    public async Task Matches_register_path_to_RegisterPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/auth/register");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("RegisterPolicy");
    }

    [Fact]
    public async Task Matches_refresh_path_to_RefreshPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/auth/refresh");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("RefreshPolicy");
    }

    [Fact]
    public async Task Matches_logout_path_to_LogoutPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/auth/logout");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("LogoutPolicy");
    }

    [Fact]
    public async Task Matches_me_path_to_MePolicy()
    {
        var context = Fixture.CreateContext("/api/v1/auth/me");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("MePolicy");
    }

    [Fact]
    public async Task Matches_search_videos_path_to_SearchVideosPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/yt/search");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("SearchVideosPolicy");
    }

    [Fact]
    public async Task Matches_downloads_mp3_path_to_GetDownloadLinkPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/yt/downloads/mp3");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("GetDownloadLinkPolicy");
    }

    [Fact]
    public async Task Matches_download_file_path_to_DownloadFilePolicy()
    {
        var context = Fixture.CreateContext("/api/v1/yt/downloads/file");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("DownloadFilePolicy");
    }

    [Fact]
    public async Task Matches_history_search_path_to_SearchHistoryPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/yt/search/history");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("SearchHistoryPolicy");
    }

    [Fact]
    public async Task Matches_history_downloads_path_to_DownloadHistoryPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/yt/downloads/history");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("DownloadHistoryPolicy");
    }

    [Fact]
    public async Task Matches_history_root_path_to_GetHistoryPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/yt/history");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("GetHistoryPolicy");
    }

    [Fact]
    public async Task Matches_preview_path_to_PreviewPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/yt/preview");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("PreviewPolicy");
    }

    [Fact]
    public async Task Matches_assistant_chat_path_to_AssistantPolicy()
    {
        var context = CreateContext("/api/v1/yt/assistant/chat");
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("AssistantPolicy");
    }

    [Fact]
    public async Task Matches_health_path_to_HealthPolicy()
    {
        var context = Fixture.CreateContext("/health");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("HealthPolicy");
    }

    [Fact]
    public async Task Matches_health_live_path_to_HealthPolicy()
    {
        var context = Fixture.CreateContext("/health/live");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("HealthPolicy");
    }

    [Fact]
    public async Task Matches_health_ready_path_to_HealthPolicy()
    {
        var context = Fixture.CreateContext("/health/ready");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("HealthPolicy");
    }

    [Fact]
    public async Task Matches_scheduler_path_to_SchedulerPolicy()
    {
        var context = Fixture.CreateContext("/api/v1/scheduler/appointments");
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.DisplayName.Should().Be("SchedulerPolicy");
    }

    [Fact]
    public async Task Returns_403_for_unmatched_path_with_endpoint()
    {
        var context = Fixture.CreateContext("/api/v1/unknown/endpoint");
        // Simulate that routing matched an endpoint — middleware should still
        // reject with 403 (fail closed) since no rate limit policy exists.
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "unmatched-route"));
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Matches_imagetools_prefix_path_without_rate_limiting()
    {
        var context = Fixture.CreateContext("/api/v1/imagetools/resize");
        // Simulate YARP having routed this request (proxied route has an endpoint)
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "imagetools-route"));
        var nextCalled = false;
        var fixture = new Fixture(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await fixture.Sut.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Matches_imagetools_root_path_without_rate_limiting()
    {
        var context = Fixture.CreateContext("/api/v1/imagetools");
        // Simulate YARP having routed this request (proxied route has an endpoint)
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "imagetools-route"));
        var nextCalled = false;
        var fixture = new Fixture(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await fixture.Sut.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Returns_proper_ApiErrorResponse_for_403_body()
    {
        var context = Fixture.CreateContext("/api/v1/unknown/endpoint");
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "unmatched-route"));
        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().NotBeNullOrEmpty();

        var deserialized = JsonSerializer.Deserialize<ApiErrorResponse>(body, ApiErrorResponse.JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Error.Should().Be("No rate limit policy configured for this endpoint");
    }

    [Fact]
    public async Task Calls_next_for_matched_path()
    {
        var nextCalled = false;
        var context = Fixture.CreateContext("/api/v1/auth/login");
        var fixture = new Fixture(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await fixture.Sut.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Calls_next_for_unmatched_path_with_no_endpoint()
    {
        // When routing doesn't match a route (no endpoint), the middleware
        // should not block — let the framework return 404.
        var nextCalled = false;
        var context = Fixture.CreateContext("/api/v1/unknown/endpoint");
        var fixture = new Fixture(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await fixture.Sut.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Does_not_call_next_for_unmatched_path_with_endpoint()
    {
        // When routing DID match an endpoint but no rate limit policy exists,
        // the middleware should short-circuit with 403 (fail closed).
        var nextCalled = false;
        var context = Fixture.CreateContext("/api/v1/unknown/endpoint");
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            EndpointMetadataCollection.Empty,
            "unmatched-route"));
        var fixture = new Fixture(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await fixture.Sut.InvokeAsync(context);

        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Preserves_original_endpoint_request_delegate_when_setting_rate_limit_policy()
    {
        // This test verifies the fix for the Gateway response body serialization bug.
        // Previously, AttachRateLimitPolicy replaced the entire endpoint (including
        // the real RequestDelegate) with a no-op (_ => Task.CompletedTask), causing
        // ALL response bodies to be empty. The fix must preserve the original endpoint's
        // RequestDelegate and only append rate limiting metadata.
        var originalHandlerInvoked = false;
        var originalEndpoint = new Endpoint(
            _ =>
            {
                originalHandlerInvoked = true;
                return Task.CompletedTask;
            },
            new EndpointMetadataCollection(new object[] { "test-metadata" }),
            "original-endpoint-preserve-delegate");

        var context = Fixture.CreateContext("/api/v1/auth/login");
        context.SetEndpoint(originalEndpoint);

        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint()!;
        endpoint.Should().NotBeNull();
        endpoint.DisplayName.Should().Be("original-endpoint-preserve-delegate");

        await endpoint.RequestDelegate(context);
        originalHandlerInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task Adds_rate_limiting_metadata_when_preserving_original_endpoint()
    {
        var originalEndpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new object[] { "test-metadata" }),
            "original-endpoint");

        var context = Fixture.CreateContext("/api/v1/auth/login");
        context.SetEndpoint(originalEndpoint);

        var fixture = Fixture.Init();

        await fixture.Sut.InvokeAsync(context);

        var endpoint = context.GetEndpoint();
        endpoint.Should().NotBeNull();
        endpoint!.Metadata.GetMetadata<EnableRateLimitingAttribute>().Should().NotBeNull();
        endpoint!.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName.Should().Be("LoginPolicy");

        endpoint!.Metadata.First(m => m is string).Should().Be("test-metadata");
    }
}