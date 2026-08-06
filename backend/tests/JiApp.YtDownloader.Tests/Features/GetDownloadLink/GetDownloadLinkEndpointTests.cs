using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using FluentValidation;
using FluentValidation.Results;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.GetDownloadLink;
using JiApp.YtDownloader.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.GetDownloadLink;

public sealed class GetDownloadLinkEndpointTests
{
    private static DownloadRequest CreateRequest() =>
        new("dQw4w9WgXcQ", "https://youtube.com/watch?v=dQw4w9WgXcQ",
            "Title", "Description", "https://example.com/img.jpg");

    [Fact]
    public async Task Validates_WithTheRequestCancellationToken()
    {
        var validator = new Mock<IValidator<DownloadRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("VideoId", "invalid video id")]));
        var handler = new GetDownloadLinkHandler(
            Mock.Of<IDownloadJobStore>(),
            Channel.CreateUnbounded<string>(),
            Mock.Of<ICurrentUserService>(x => x.UserId == 42));

        await using var app = CreateHost(services =>
        {
            services.AddSingleton(validator.Object);
            services.AddSingleton(handler);
            services.AddSingleton(new Settings { App = new Settings.AppSettings() });
        });
        app.MapGetDownloadLink();
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/downloads/mp3", CreateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        validator.Verify(v => v.ValidateAsync(
            It.IsAny<DownloadRequest>(),
            It.Is<CancellationToken>(t => t.CanBeCanceled)),
            Times.Once);
    }

    private static WebApplication CreateHost(Action<IServiceCollection> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        configure(builder.Services);
        // The mapped endpoint carries .RequireAuthorization() metadata; the test
        // exercises validation, not auth, so allow every principal through.
        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        });
        var app = builder.Build();
        app.UseAuthorization();
        return app;
    }
}
