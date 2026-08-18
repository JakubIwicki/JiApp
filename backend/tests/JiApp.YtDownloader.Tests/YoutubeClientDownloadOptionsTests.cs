using JiApp.YtApi.Clients;

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
    public void BuildDownloadArgs_LocksPlayerClientToKnownGoodTvClient()
    {
        var fixture = Fixture.Create();

        var args = fixture.Sut.BuildDownloadArgs("/tmp/t.%(ext)s", includeTvClientExtractorArgs: true);

        // Regression guard for the PR #163 fix: an android_vr player client 403s media URLs,
        // so reverting the extractor-args allow-list value must fail this test.
        args.Should().Contain("--extractor-args");
        args.Should().Contain("youtube:player_client=tv");
        args.Should().NotContain("youtube:player_client=android_vr");
    }

    [Fact]
    public void BuildDownloadArgs_Fallback_DropsExtractorArgs()
    {
        var fixture = Fixture.Create();

        var args = fixture.Sut.BuildDownloadArgs("/tmp/t.%(ext)s", includeTvClientExtractorArgs: false);

        // The fallback must not override the extractor client at all, so the config-file
        // POT args from /etc/yt-dlp.conf stay active.
        args.Should().NotContain("--extractor-args");
        args.Should().NotContain(x => x.StartsWith("youtube:player_client=", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildDownloadArgs_ReturnsMp3AudioDownload_WithPlaylistDisabledAndChildBounds()
    {
        var fixture = Fixture.Create();

        var args = fixture.Sut.BuildDownloadArgs("/tmp/t.%(ext)s", includeTvClientExtractorArgs: true);

        args.Should().Contain("--no-playlist");
        args.Should().Contain("-x");
        args.Should().Contain("--audio-format");
        args.Should().Contain("mp3");
        args.Should().Contain("--embed-thumbnail");
        args.Should().Contain("--embed-metadata");
        args.Should().Contain("-o");
        args.Should().Contain("/tmp/t.%(ext)s");
        args.Should().Contain("--retries");
        args.Should().Contain("2");
        args.Should().Contain("--fragment-retries");
        args.Should().Contain("2");
        args.Should().Contain("--socket-timeout");
        args.Should().Contain("15");
        args.Should().Contain("--max-filesize");
        args.Should().Contain("500M");
    }

    [Fact]
    public void BuildDownloadArgs_CookiesFromBrowser_WinsOverCookiesFile()
    {
        var fixture = Fixture.Create(cookiesFile: "/tmp/cookies.txt", cookiesFromBrowser: "madeupbrowser");

        var args = fixture.Sut.BuildDownloadArgs("/tmp/t.%(ext)s", includeTvClientExtractorArgs: true);

        args.Should().Contain("--cookies-from-browser");
        args.Should().Contain("madeupbrowser");
        args.Should().NotContain("--cookies");
        args.Should().NotContain("/tmp/cookies.txt");
    }

    [Fact]
    public void BuildDownloadArgs_IncludesCookiesFile_WhenConfiguredWithoutBrowser()
    {
        var fixture = Fixture.Create(cookiesFile: "/tmp/cookies.txt");

        var args = fixture.Sut.BuildDownloadArgs("/tmp/t.%(ext)s", includeTvClientExtractorArgs: true);

        args.Should().Contain("--cookies");
        args.Should().Contain("/tmp/cookies.txt");
        args.Should().NotContain("--cookies-from-browser");
    }

    [Fact]
    public void BuildDownloadArgs_IncludesProxy_WhenConfigured()
    {
        var fixture = Fixture.Create(proxy: "socks5://127.0.0.1:1080");

        var args = fixture.Sut.BuildDownloadArgs("/tmp/t.%(ext)s", includeTvClientExtractorArgs: true);

        args.Should().Contain("--proxy");
        args.Should().Contain("socks5://127.0.0.1:1080");
    }
}
