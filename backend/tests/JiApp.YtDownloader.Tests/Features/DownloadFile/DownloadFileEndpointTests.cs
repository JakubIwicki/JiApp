using System.Net;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Features.DownloadFile;
using JiApp.YtDownloader.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.DownloadFile;

public sealed class DownloadFileEndpointTests
{
    private const long UserId = 42L;

    [Fact]
    public async Task Succeeds_WithAudioMpeg_WhenFileExists()
    {
        var filePath = Path.GetTempFileName();
        File.WriteAllBytes(filePath, [0x49, 0x44, 0x33]);
        var handler = new DownloadFileHandler(
            Mock.Of<IDownloadJobStore>(s => s.GetFilePath("abc", UserId) == filePath),
            Mock.Of<ICurrentUserService>(x => x.UserId == UserId),
            NullLogger<DownloadFileHandler>.Instance);

        await using var app = CreateHost(services => services.AddSingleton(handler));
        app.MapDownloadFile();
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/downloads/mp3/file/abc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("audio/mpeg");
        File.Delete(filePath);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenFileMissing()
    {
        var handler = new DownloadFileHandler(
            Mock.Of<IDownloadJobStore>(),
            Mock.Of<ICurrentUserService>(x => x.UserId == UserId),
            NullLogger<DownloadFileHandler>.Instance);

        await using var app = CreateHost(services => services.AddSingleton(handler));
        app.MapDownloadFile();
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/downloads/mp3/file/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static WebApplication CreateHost(Action<IServiceCollection> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        configure(builder.Services);
        // The mapped endpoint carries .RequireAuthorization() metadata; the test
        // exercises the Result->HTTP mapping, not auth, so allow every principal through.
        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        });
        var app = builder.Build();
        app.UseAuthorization();
        return app;
    }
}
