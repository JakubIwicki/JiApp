using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using FluentValidation.Results;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.SearchVideos;
using JiApp.YtDownloader.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.SearchVideos;

public sealed class SearchVideosEndpointTests
{
    [Fact]
    public async Task Validates_WithTheRequestCancellationToken()
    {
        var validator = new Mock<IValidator<SearchVideosRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<SearchVideosRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Query", "invalid query")]));
        var handler = new SearchVideosHandler(
            Mock.Of<IYoutubeClient>(),
            Mock.Of<ISearchHistoryRepository>(),
            Mock.Of<ICurrentUserService>(x => x.UserId == 42),
            Mock.Of<IMemoryCache>(),
            new Settings { App = new Settings.AppSettings(), Youtube = new Settings.YoutubeSettings() },
            NullLogger<SearchVideosHandler>.Instance,
            TimeProvider.System);

        await using var app = CreateHost(services =>
        {
            services.AddSingleton(validator.Object);
            services.AddSingleton(handler);
        });
        app.MapSearchVideos();
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/search", new SearchVideosRequest("test query", Page: null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        validator.Verify(v => v.ValidateAsync(
            It.IsAny<SearchVideosRequest>(),
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
