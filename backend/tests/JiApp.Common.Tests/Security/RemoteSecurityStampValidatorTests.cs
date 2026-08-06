using System.Net;
using System.Reflection;
using JiApp.Common.Resilience;
using JiApp.Common.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JiApp.Common.Tests.Security;

public sealed class RemoteSecurityStampValidatorTests
{
    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public Exception? ThrowException { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ThrowException is not null)
                throw ThrowException;

            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class CountingHttpMessageHandler(int transientFailures) : HttpMessageHandler
    {
        private readonly List<HttpRequestMessage> _requests = [];
        private readonly List<TrackingHttpResponseMessage> _responses = [];

        public IReadOnlyList<HttpRequestMessage> Requests => _requests;
        public IReadOnlyList<TrackingHttpResponseMessage> Responses => _responses;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request);

            if (_requests.Count <= transientFailures)
                throw new HttpRequestException("transient identity failure");

            var response = new TrackingHttpResponseMessage(HttpStatusCode.OK);
            _responses.Add(response);
            return Task.FromResult<HttpResponseMessage>(response);
        }
    }

    private sealed class TrackingHttpResponseMessage(HttpStatusCode statusCode) : HttpResponseMessage(statusCode)
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private static ILogger<T> NullLogger<T>() => NullLoggerFactory.Instance.CreateLogger<T>();

    private static IRetryPolicyFactory RetryPolicy => new RetryPolicyFactory(TimeProvider.System);

    private static IHttpContextAccessor CreateHttpContextAccessor(string? authHeader = "Bearer testtoken")
    {
        var httpContext = new DefaultHttpContext();
        if (authHeader is not null)
            httpContext.Request.Headers.Authorization = authHeader;
        return new HttpContextAccessor { HttpContext = httpContext };
    }

    private static bool IsRequestDisposed(HttpRequestMessage request)
    {
        // HttpRequestMessage exposes no public disposed state, so we read the
        // internal _sendStatus field, which the runtime moves into the disposed
        // range (4 for an unsent request, 5 for one already handed to HttpClient)
        // once Dispose() has run. Disposed values are >= 4; live requests are 0-1.
        var sendStatus = typeof(HttpRequestMessage)
            .GetField("_sendStatus", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(request);
        return (int)sendStatus! >= 4;
    }

    [Fact]
    public async Task ValidateCurrentAsync_204Response_ReturnsValid()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.NoContent);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
        var accessor = CreateHttpContextAccessor();
        var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>(), RetryPolicy);

        var result = await validator.ValidateCurrentAsync();

        result.Should().Be(StampValidationResult.Valid);
    }

    [Fact]
    public async Task ValidateCurrentAsync_401Response_ReturnsRevoked()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.Unauthorized);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
        var accessor = CreateHttpContextAccessor();
        var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>(), RetryPolicy);

        var result = await validator.ValidateCurrentAsync();

        result.Should().Be(StampValidationResult.Revoked);
    }

    [Fact]
    public async Task ValidateCurrentAsync_500Response_ReturnsUnavailable()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
        var accessor = CreateHttpContextAccessor();
        var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>(), RetryPolicy);

        var result = await validator.ValidateCurrentAsync();

        result.Should().Be(StampValidationResult.Unavailable);
    }

    [Fact]
    public async Task ValidateCurrentAsync_HttpRequestException_ReturnsUnavailable()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK)
        {
            ThrowException = new HttpRequestException("Connection refused")
        };
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
        var accessor = CreateHttpContextAccessor();
        var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>(), RetryPolicy);

        var result = await validator.ValidateCurrentAsync();

        result.Should().Be(StampValidationResult.Unavailable);
    }

    [Fact]
    public async Task ValidateCurrentAsync_NoAuthorizationHeader_ReturnsUnavailable()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.NoContent);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
        var accessor = CreateHttpContextAccessor(authHeader: null);
        var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>(), RetryPolicy);

        var result = await validator.ValidateCurrentAsync();

        result.Should().Be(StampValidationResult.Unavailable);
    }

    [Fact]
    public async Task ValidateCurrentAsync_TaskCanceledException_Timeout_ReturnsUnavailable()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK)
        {
            ThrowException = new TaskCanceledException()
        };
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
        var accessor = CreateHttpContextAccessor();
        var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>(), RetryPolicy);

        var result = await validator.ValidateCurrentAsync();

        result.Should().Be(StampValidationResult.Unavailable);
    }

    [Fact]
    public async Task ValidateCurrentAsync_NoHttpContext_ReturnsUnavailable()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.NoContent);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
        var accessor = new HttpContextAccessor { HttpContext = null! }; // no HttpContext
        var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>(), RetryPolicy);

        var result = await validator.ValidateCurrentAsync();

        result.Should().Be(StampValidationResult.Unavailable);
    }

    [Fact]
    public async Task ValidateCurrentAsync_CallerCancelled_ReturnsUnavailable()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.NoContent);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
        var accessor = CreateHttpContextAccessor();
        var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>(), RetryPolicy);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await validator.ValidateCurrentAsync(cts.Token);

        result.Should().Be(StampValidationResult.Unavailable);
    }

    [Fact]
    public async Task ValidateCurrentAsync_Retries_DisposesRequestAndResponsePerAttempt()
    {
        var handler = new CountingHttpMessageHandler(transientFailures: 1);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
        var accessor = CreateHttpContextAccessor();
        var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>(), RetryPolicy);

        var result = await validator.ValidateCurrentAsync();

        result.Should().Be(StampValidationResult.Valid);
        handler.Requests.Should().HaveCount(2);                    // one fresh request per attempt
        handler.Requests.Should().OnlyContain(r => IsRequestDisposed(r));
        handler.Responses.Should().ContainSingle();
        handler.Responses[0].Disposed.Should().BeTrue();
    }
}
