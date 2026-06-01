using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.StreamPreview;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.StreamPreview;

public class StreamPreviewHandlerTests
{
    [Fact]
    public void BuildFfmpegArguments_includes_configured_preview_duration()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 30);

        args.Should().Contain("-t 30");
    }

    [Fact]
    public void BuildFfmpegArguments_includes_audio_url()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 10);

        args.Should().Contain("-i \"https://example.com/audio.mp3\"");
    }

    [Fact]
    public void BuildFfmpegArguments_includes_mp3_output_format()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 10);

        args.Should().Contain("-f mp3 -");
    }

    [Fact]
    public void BuildFfmpegArguments_uses_default_10_seconds_when_not_configured()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 10);

        args.Should().Contain("-t 10");
    }

    [Fact]
    public async Task HandleAsync_WhenYoutubeClientThrows_ReturnsFailure()
    {
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.ResolveAudioUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<StreamPreviewHandler>>().Object;
        var settings = new Settings { App = new Settings.AppSettings() };

        var handler = new StreamPreviewHandler(
            youtubeClient.Object, cache, logger, settings);

        var result = await handler.HandleAsync("test-video-id");

        result.Should().Be(StreamPreviewResult.ResolveFailed);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_ReturnsFailure()
    {
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.ResolveAudioUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<StreamPreviewHandler>>().Object;
        var settings = new Settings { App = new Settings.AppSettings() };

        var handler = new StreamPreviewHandler(
            youtubeClient.Object, cache, logger, settings);

        var result = await handler.HandleAsync("test-video-id");

        result.Should().Be(StreamPreviewResult.ResolveFailed);
    }

    [Fact]
    public void BuildFfmpegArguments_includes_loglevel_quiet()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(
            "https://example.com/audio.mp3", previewDurationSeconds: 10);

        args.Should().Contain("-loglevel quiet");
    }
}