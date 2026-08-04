#pragma warning disable ASPDEPR004, ASPDEPR008 // WebHostBuilder/GetTestClient obsolete in production but required for WSL test infra
using JiApp.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JiApp.Common.Tests.Middleware;

public sealed class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_OnUnhandledException_Returns500Json()
    {
        var logger = new RecordingLogger<GlobalExceptionMiddleware>();
        using var client = CreateClient(_ => throw new InvalidOperationException("boom"), logger);

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("An unexpected error occurred");
        logger.AssertLoggedError("boom");
    }

    [Fact]
    public async Task InvokeAsync_OnUnhandledException_InDevelopment_ReturnsExceptionMessage()
    {
        var logger = new RecordingLogger<GlobalExceptionMiddleware>();
        using var client = CreateClient(_ => throw new InvalidOperationException("boom"), logger, environment: "Development");

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("boom");
    }

    [Fact]
    public async Task InvokeAsync_OnUnauthorizedAccessException_Returns401()
    {
        var logger = new RecordingLogger<GlobalExceptionMiddleware>();
        using var client = CreateClient(_ => throw new UnauthorizedAccessException(), logger);

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Unauthorized");
        logger.AssertNoErrors();
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasStarted_RethrowsOriginalError()
    {
        var logger = new RecordingLogger<GlobalExceptionMiddleware>();
        using var client = CreateClient(async ctx =>
        {
            await ctx.Response.WriteAsync("partial");
            throw new InvalidOperationException("stream boom");
        }, logger);

        HttpResponseMessage? response = null;
        Exception? thrown = null;
        try
        {
            response = await client.GetAsync("/");
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        if (response is null)
        {
            // Post-fix the middleware rethrows the original error, whose base message is
            // "stream boom". Pre-fix it instead threw the masking "Headers are read-only,
            // response has already started" InvalidOperationException from setting
            // StatusCode on a started response — so this assertion fails without the guard.
            thrown.Should().NotBeNull();
            thrown!.GetBaseException().Message.Should().Contain("stream boom");
        }
        else
        {
            // TestServer returned the partial stream — the middleware never wrote the
            // JSON error over the started response, which itself implies the guard held.
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Be("partial");
            body.Should().NotContain("An unexpected error occurred");
        }

        logger.AssertLoggedError("stream boom");
        logger.AssertNoError("response has already started");
    }

    [Fact]
    public async Task InvokeAsync_OnOperationCanceledException_IsNotSwallowed()
    {
        var logger = new RecordingLogger<GlobalExceptionMiddleware>();
        using var client = CreateClient(_ => throw new OperationCanceledException(), logger);

        try
        {
            var response = await client.GetAsync("/");
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain("An unexpected error occurred");
        }
        catch (Exception)
        {
            // TestServer surfaces the unhandled cancellation as a transport error — acceptable.
        }

        logger.AssertNoErrors();
    }

    private static HttpClient CreateClient(
        RequestDelegate terminal, RecordingLogger<GlobalExceptionMiddleware> logger, string environment = "Production")
    {
        var host = new WebHostBuilder()
            .UseTestServer()
            .UseEnvironment(environment)
            .ConfigureServices(services =>
            {
                services.AddSingleton<ILogger<GlobalExceptionMiddleware>>(logger);
                services.AddTransient<GlobalExceptionMiddleware>();
            })
            .Configure(app =>
            {
                app.UseMiddleware<GlobalExceptionMiddleware>();
                app.Run(terminal);
            })
            .Start();

        return host.GetTestClient();
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel Level, string Message, Exception? Exception)> _entries = [];

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
                _entries.Add((logLevel, formatter(state, exception), exception));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void AssertLoggedError(string messageFragment)
            => Assert.Contains(_entries,
                e => e.Level == LogLevel.Error && e.Exception is not null && e.Exception.Message.Contains(messageFragment));

        public void AssertNoError(string messageFragment)
            => Assert.DoesNotContain(_entries,
                e => e.Level == LogLevel.Error && e.Exception is not null && e.Exception.Message.Contains(messageFragment));

        public void AssertNoErrors()
            => Assert.DoesNotContain(_entries, e => e.Level == LogLevel.Error);
    }
}
