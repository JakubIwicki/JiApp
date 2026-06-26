using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.StreamPreview;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.StreamPreview;

public sealed class StreamPreviewHandlerTests
{
    private sealed class Fixture
    {
        public Mock<IYoutubeClient> YoutubeClientMock { get; } = new();

        public StreamPreviewHandler Sut { get; }

        public Fixture()
        {
            Sut = new StreamPreviewHandler(
                YoutubeClientMock.Object, Mock.Of<ILogger<StreamPreviewHandler>>(), new Settings { App = new Settings.AppSettings() });
        }

        public Fixture WithBuildPreviewThrows(Exception exception)
        {
            YoutubeClientMock
                .Setup(c => c.BuildPreviewAudioProcess(It.IsAny<string>()))
                .Throws(exception);
            return this;
        }
    }

    [Fact]
    public void BuildFfmpegArguments_IncludesConfiguredPreviewDuration()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(previewDurationSeconds: 30);

        args.Should().Contain("-t 30");
    }

    [Fact]
    public void BuildFfmpegArguments_IncludesPipe0Input()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(previewDurationSeconds: 10);

        args.Should().Contain("-i pipe:0");
    }

    [Fact]
    public void BuildFfmpegArguments_IncludesMp3OutputFormat()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(previewDurationSeconds: 10);

        args.Should().Contain("-f mp3 -");
    }

    [Fact]
    public void BuildFfmpegArguments_UsesPassedDuration()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(previewDurationSeconds: 10);

        args.Should().Contain("-t 10");
    }

    [Fact]
    public void BuildFfmpegArguments_IncludesLoglevelQuiet()
    {
        var args = StreamPreviewHandler.BuildFfmpegArguments(previewDurationSeconds: 10);

        args.Should().Contain("-loglevel quiet");
    }

    [Fact]
    public void HandleAsync_WhenYoutubeClientThrowsArgumentException_ReturnsResolveFailed()
    {
        var fixture = new Fixture().WithBuildPreviewThrows(new ArgumentException("Invalid videoId: 'bad'"));

        var result = fixture.Sut.Handle("bad");

        result.Should().Be(StreamPreviewResult.ResolveFailed);
    }
}
