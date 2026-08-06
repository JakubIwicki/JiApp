using System.Net;
using api.JiApp.LovingBoards.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace api.JiApp.LovingBoards.Tests.Clients;

/// <summary>
/// Transport-level coverage for <see cref="UserExistenceClient"/> — real HttpClient
/// through a stubbed handler, with a current HttpContext carrying the caller's
/// Authorization header. The handler doubles used by the feature tests would never
/// have caught the missing-token 401 bug this class guards against.
/// </summary>
public sealed class UserExistenceClientTests
{
    [Fact]
    public async Task ReturnsFound_WhenIdentityReturns200()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var status = await client.CheckExistsAsync(42, CancellationToken.None);

        status.Should().Be(UserExistenceStatus.Found);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenIdentityReturns404()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var status = await client.CheckExistsAsync(42, CancellationToken.None);

        status.Should().Be(UserExistenceStatus.NotFound);
    }

    [Fact]
    public async Task ReturnsUnavailable_WhenIdentityReturns401()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var status = await client.CheckExistsAsync(42, CancellationToken.None);

        status.Should().Be(UserExistenceStatus.Unavailable);
    }

    [Fact]
    public async Task ReturnsUnavailable_WhenIdentityReturns500()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var status = await client.CheckExistsAsync(42, CancellationToken.None);

        status.Should().Be(UserExistenceStatus.Unavailable);
    }

    [Fact]
    public async Task ReturnsUnavailable_WhenIdentityThrowsHttpRequestException()
    {
        var (client, _) = CreateClient(_ => throw new HttpRequestException("connection refused"));

        var status = await client.CheckExistsAsync(42, CancellationToken.None);

        status.Should().Be(UserExistenceStatus.Unavailable);
    }

    [Fact]
    public async Task ReturnsUnavailable_WhenIdentityTimesOut()
    {
        var (client, _) = CreateClient(_ => throw new TaskCanceledException("timeout"));

        var status = await client.CheckExistsAsync(42, CancellationToken.None);

        status.Should().Be(UserExistenceStatus.Unavailable);
    }

    [Fact]
    public async Task ForwardsCallersAuthorizationHeader()
    {
        var (client, handler) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await client.CheckExistsAsync(42, CancellationToken.None);

        var sent = handler.Requests.Should().ContainSingle().Subject;
        sent.Headers.Authorization.Should().NotBeNull();
        sent.Headers.Authorization!.Scheme.Should().Be("Bearer");
        sent.Headers.Authorization!.Parameter.Should().Be("test-token");
    }

    [Fact]
    public async Task ReturnsUnavailable_WhenNoAuthorizationHeader_AndSendsNoRequest()
    {
        var (client, handler) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK), authorizationHeader: null);

        var status = await client.CheckExistsAsync(42, CancellationToken.None);

        status.Should().Be(UserExistenceStatus.Unavailable);
        handler.Requests.Should().BeEmpty();
    }

    private static (UserExistenceClient Client, StubHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        string? authorizationHeader = "Bearer test-token")
    {
        var handler = new StubHttpMessageHandler(respond);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var httpContext = new DefaultHttpContext();
        if (authorizationHeader is not null)
            httpContext.Request.Headers.Authorization = authorizationHeader;
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var client = new UserExistenceClient(httpClient, accessor, NullLogger<UserExistenceClient>.Instance);
        return (client, handler);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }
}
