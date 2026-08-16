using JiApp.YtApi.Clients;
using YoutubeDLSharp.Options;

namespace JiApp.YtDownloader.Tests;

public sealed class YoutubeClientDownloadOptionsTests
{
    private sealed class Fixture
    {
        public YoutubeClient Sut { get; }

        public Fixture(string? cookiesFile = null, string? cookiesFromBrowser = null, string? proxy = null)
        {
            Sut = new YoutubeClient("fake-key", "yt-dlp", "ffmpeg", cookiesFile, cookiesFromBrowser, proxy);
        }

        public static Fixture Create(string? cookiesFile = null, string? cookiesFromBrowser = null, string? proxy = null) =>
            new(cookiesFile, cookiesFromBrowser, proxy);
    }

    [Fact]
    public void BuildDownloadOptions_LocksPlayerClientToKnownGoodTvClient()
    {
        var fixture = Fixture.Create();

        var options = fixture.Sut.BuildDownloadOptions("/tmp/t.%(ext)s");

        // Regression guard for the PR #163 fix: an android_vr player client 403s media URLs,
        // so reverting the extractor-args allow-list value must fail this test.
        options.ExtractorArgs.Values.Should().ContainSingle().Which.Should().Be("youtube:player_client=tv");
    }

    [Fact]
    public void BuildDownloadOptions_ReturnsMp3AudioDownloadWithPlaylistDisabled()
    {
        var fixture = Fixture.Create();

        var options = fixture.Sut.BuildDownloadOptions("/tmp/t.%(ext)s");

        options.NoPlaylist.Should().BeTrue();
        options.ExtractAudio.Should().BeTrue();
        options.AudioFormat.Should().Be(AudioConversionFormat.Mp3);
    }
}
