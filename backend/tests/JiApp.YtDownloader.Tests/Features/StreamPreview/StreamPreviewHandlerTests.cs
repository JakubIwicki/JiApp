using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.StreamPreview;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.StreamPreview;

public sealed class StreamPreviewHandlerTests
{
    private sealed class Fixture
    {
        public Mock<IYoutubeClient> YoutubeClientMock { get; } = new();
        public IMemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());

        public StreamPreviewHandler Sut { get; }

        public Fixture()
        {
            Sut = new StreamPreviewHandler(
                YoutubeClientMock.Object, Cache, Mock.Of<ILogger<StreamPreviewHandler>>(), new Settings { App = new Settings.AppSettings() });
        }

        public Fixture WithResolveThrows(Exception exception)
        {
            YoutubeClientMock
                .Setup(c => c.ResolveAudioUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            return this;
        }
    }

    [Fact]
    public void BuildFfmpegArguments_IncludesConfiguredPreviewDuration()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 30);

        args.Should().Contain("-t 30");
    }

    [Fact]
    public void BuildFfmpegArguments_IncludesAudioUrl()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 10);

        args.Should().Contain("-i \"https://example.com/audio.mp3\"");
    }

    [Fact]
    public void BuildFfmpegArguments_IncludesMp3OutputFormat()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 10);

        args.Should().Contain("-f mp3 -");
    }

    [Fact]
    public void BuildFfmpegArguments_UsesDefault10Seconds_WhenNotConfigured()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 10);

        args.Should().Contain("-t 10");
    }

    [Fact]
    public async Task HandleAsync_WhenYoutubeClientThrows_ReturnsFailure()
    {
        var fixture = new Fixture().WithResolveThrows(new InvalidOperationException("Test exception"));

        var result = await fixture.Sut.HandleAsync("test-video-id");

        result.Should().Be(StreamPreviewResult.ResolveFailed);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_ReturnsFailure()
    {
        var fixture = new Fixture().WithResolveThrows(new OperationCanceledException());

        var result = await fixture.Sut.HandleAsync("test-video-id");

        result.Should().Be(StreamPreviewResult.ResolveFailed);
    }

    [Fact]
    public void BuildFfmpegArguments_IncludesLoglevelQuiet()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 10);

        args.Should().Contain("-loglevel quiet");
    }
}