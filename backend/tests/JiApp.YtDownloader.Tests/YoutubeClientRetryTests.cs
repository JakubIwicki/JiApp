using System.Net;
using System.Text;
using Google.Apis.Http;
using JiApp.Common.Resilience;
using JiApp.YtApi.Clients;
using Moq;
using Polly;
using Polly.Retry;

namespace JiApp.YtDownloader.Tests;

public sealed class YoutubeClientRetryTests
{
    [Fact]
    public async Task SearchVideosAsync_WhenYoutubeApiReturns429ThenSuccess_RetriesAndSucceeds()
    {
        var handler = new StatusQueueHandler(HttpStatusCode.TooManyRequests, HttpStatusCode.OK);
        using var client = CreateClient(handler);

        var results = await client.SearchVideosAsync("anything");

        handler.CallCount.Should().Be(2);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVideoByIdAsync_WhenYoutubeApiReturns500ThenSuccess_RetriesAndSucceeds()
    {
        var handler = new StatusQueueHandler(HttpStatusCode.InternalServerError, HttpStatusCode.OK);
        using var client = CreateClient(handler);

        var video = await client.GetVideoByIdAsync("dQw4w9WgXcQ");

        handler.CallCount.Should().Be(2);
        video.Should().BeNull();
    }

    [Fact]
    public async Task SearchVideosAsync_WhenYoutubeApiReturns403QuotaError_DoesNotRetry()
    {
        var handler = new StatusQueueHandler(HttpStatusCode.Forbidden);
        using var client = CreateClient(handler);

        var act = async () => await client.SearchVideosAsync("anything");

        await act.Should().ThrowAsync<YoutubeApiException>();
        handler.CallCount.Should().Be(1);
    }

    private static YoutubeClient CreateClient(StatusQueueHandler handler) =>
        new(
            "fake-key", "yt-dlp", "ffmpeg",
            httpClientFactory: new StubHttpClientFactory(handler),
            retryPolicyFactory: ZeroDelayRetryFactory());

    private static IRetryPolicyFactory ZeroDelayRetryFactory()
    {
        var mock = new Mock<IRetryPolicyFactory>();
        mock.Setup(f => f.RetryOnTransientHttp_WithExponentialBackoff(
                It.IsAny<int>(), It.IsAny<Func<Exception, bool>?>()))
            .Returns((int retries, Func<Exception, bool>? predicate) =>
            {
                var builder = new ResiliencePipelineBuilder();
                return builder
                    .AddRetry(new RetryStrategyOptions
                    {
                        MaxRetryAttempts = retries,
                        Delay = TimeSpan.Zero,
                        ShouldHandle = args => new ValueTask<bool>(
                            predicate?.Invoke(args.Outcome.Exception!) ?? false),
                    })
                    .Build();
            });
        return mock.Object;
    }

    private sealed class StatusQueueHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _statuses;

        public StatusQueueHandler(params HttpStatusCode[] statuses)
        {
            _statuses = new Queue<HttpStatusCode>(statuses);
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var status = _statuses.Count > 0 ? _statuses.Dequeue() : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class StubHttpClientFactory(StatusQueueHandler handler) : Google.Apis.Http.IHttpClientFactory
    {
        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args) =>
            // NumTries = 1 disables Google.Apis' internal retry loop so the only retries
            // are the ones under test (the ResiliencePipeline passed via retryPolicyFactory).
            new(new ConfigurableMessageHandler(handler) { NumTries = 1 });
    }
}
