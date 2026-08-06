using System.Net;
using System.Text;
using Google.Apis.Http;
using JiApp.YtApi.Clients;

namespace JiApp.YtDownloader.Tests;

public sealed class YoutubeClientNullGuardTests
{
    private sealed class EmptyJsonResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
    }

    private sealed class StubHttpClientFactory : Google.Apis.Http.IHttpClientFactory
    {
        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args) =>
            new(new ConfigurableMessageHandler(new EmptyJsonResponseHandler()));
    }

    private static YoutubeClient CreateClient() =>
        new("fake-key", "yt-dlp", "ffmpeg", httpClientFactory: new StubHttpClientFactory());

    [Fact]
    public async Task SearchVideosAsync_WhenResponseHasNoItems_ReturnsEmptyList()
    {
        using var client = CreateClient();

        var results = await client.SearchVideosAsync("anything");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVideoByIdAsync_WhenResponseHasNoItems_ReturnsNull()
    {
        using var client = CreateClient();

        var video = await client.GetVideoByIdAsync("dQw4w9WgXcQ");

        video.Should().BeNull();
    }
}
