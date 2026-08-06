using JiApp.YtApi.Clients;
using YoutubeDLSharp.Options;

namespace JiApp.YtDownloader.Tests;

public sealed class YoutubeClientValidationTests
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

    [Theory]
    [InlineData("")]
    [InlineData("abc")] // too short
    [InlineData("abcdefghijklm")] // too long (13)
    [InlineData("abc-def-abc-")] // wrong length (12, but ok pattern)
    [InlineData("abc def ghijk")] // contains space
    [InlineData("<script>")] // XSS payload
    [InlineData("../../etc/passwd")] // path traversal
    [InlineData("--exec=rm -rf /")] // argument injection
    public void BuildPreviewAudioProcess_ThrowsArgumentException_ForInvalidVideoId(string invalidVideoId)
    {
        var fixture = Fixture.Create();

        var act = () => fixture.Sut.BuildPreviewAudioProcess(invalidVideoId);

        act.Should().ThrowExactly<ArgumentException>()
            .And.Message.Should().Contain("videoId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")] // too short
    [InlineData("abcdefghijklm")] // too long (13)
    [InlineData("abc def ghijk")] // contains space
    [InlineData("--exec=rm -rf /")] // argument injection
    public async Task DownloadVideoAsync_ReturnsTypedFailure_ForInvalidVideoId(string invalidVideoId)
    {
        var fixture = Fixture.Create();

        var result = await fixture.Sut.DownloadVideoAsync(invalidVideoId, "/tmp/out", "job-temp-id");

        result.Success.Should().BeFalse();
        result.FilePath.Should().BeNull();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ResolveOutputFilePath_ReturnsReportedPath_WhenItExists()
    {
        var outputPath = Directory.CreateTempSubdirectory("ytdl-resolve-").FullName;
        try
        {
            var reported = Path.Combine(outputPath, "reported.mp3");
            File.WriteAllBytes(reported, [1]);

            var resolved = YoutubeClient.ResolveOutputFilePath(outputPath, "job-abc", reported);

            resolved.Should().Be(reported);
        }
        finally
        {
            Directory.Delete(outputPath, recursive: true);
        }
    }

    [Fact]
    public void ResolveOutputFilePath_PrefersJobKeyedFile_OverNewestMp3()
    {
        // The fallback must be deterministic per job: a newer decoy *.mp3 in the same
        // folder must NOT win — that glob is how two concurrent downloads cross-resolve.
        var outputPath = Directory.CreateTempSubdirectory("ytdl-resolve-").FullName;
        try
        {
            var decoy = Path.Combine(outputPath, "newest.mp3");
            var jobFile = Path.Combine(outputPath, "job-abc.mp3");
            File.WriteAllBytes(decoy, [1]);
            File.WriteAllBytes(jobFile, [2]);
            File.SetLastWriteTimeUtc(decoy, DateTime.UtcNow);
            File.SetLastWriteTimeUtc(jobFile, DateTime.UtcNow.AddHours(-1));

            var resolved = YoutubeClient.ResolveOutputFilePath(outputPath, "job-abc", ytDlpReportedPath: null);

            resolved.Should().Be(jobFile);
        }
        finally
        {
            Directory.Delete(outputPath, recursive: true);
        }
    }

    [Fact]
    public void ResolveOutputFilePath_ReturnsNull_WhenNoFileMatches()
    {
        var outputPath = Directory.CreateTempSubdirectory("ytdl-resolve-").FullName;
        try
        {
            var resolved = YoutubeClient.ResolveOutputFilePath(outputPath, "job-abc", ytDlpReportedPath: null);

            resolved.Should().BeNull();
        }
        finally
        {
            Directory.Delete(outputPath, recursive: true);
        }
    }

    [Fact]
    public void BuildPreviewAudioProcess_ReturnsProcess_WithCorrectArgs()
    {
        var fixture = Fixture.Create();

        var process = fixture.Sut.BuildPreviewAudioProcess("dQw4w9WgXcQ");

        process.StartInfo.ArgumentList.Should().Contain("--no-playlist");
        process.StartInfo.ArgumentList.Should().NotContain("youtube:player_client=android_vr");
        process.StartInfo.ArgumentList.Should().NotContain("--extractor-args");
        process.StartInfo.ArgumentList.Should().Contain("bestaudio[ext=webm]/bestaudio");
        process.StartInfo.ArgumentList.Should().Contain("-o");
        process.StartInfo.ArgumentList.Should().Contain("-");
        process.StartInfo.ArgumentList.Should()
            .Contain("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }

    [Fact]
    public void BuildPreviewAudioProcess_IncludesCookiesFromBrowser_WhenConfigured()
    {
        var fixture = Fixture.Create(cookiesFromBrowser: "madeupbrowser");

        var process = fixture.Sut.BuildPreviewAudioProcess("dQw4w9WgXcQ");

        process.StartInfo.ArgumentList.Should().Contain("--cookies-from-browser");
        process.StartInfo.ArgumentList.Should().Contain("madeupbrowser");
        process.StartInfo.ArgumentList.Should().NotContain("--cookies");
    }

    [Fact]
    public void BuildPreviewAudioProcess_IncludesCookiesFile_WhenConfigured()
    {
        var fixture = Fixture.Create(cookiesFile: "/tmp/cookies.txt");

        var process = fixture.Sut.BuildPreviewAudioProcess("dQw4w9WgXcQ");

        process.StartInfo.ArgumentList.Should().Contain("--cookies");
        process.StartInfo.ArgumentList.Should().Contain("/tmp/cookies.txt");
        process.StartInfo.ArgumentList.Should().NotContain("--cookies-from-browser");
    }

    [Fact]
    public void BuildPreviewAudioProcess_CookiesFromBrowser_WinsOverCookiesFile()
    {
        var fixture = Fixture.Create(
            cookiesFile: "/tmp/cookies.txt",
            cookiesFromBrowser: "madeupbrowser");

        var process = fixture.Sut.BuildPreviewAudioProcess("dQw4w9WgXcQ");

        process.StartInfo.ArgumentList.Should().Contain("--cookies-from-browser");
        process.StartInfo.ArgumentList.Should().Contain("madeupbrowser");
        process.StartInfo.ArgumentList.Should().NotContain("--cookies");
        process.StartInfo.ArgumentList.Should().NotContain("/tmp/cookies.txt");
    }

    [Fact]
    public void BuildPreviewAudioProcess_IncludesProxy_WhenConfigured()
    {
        var fixture = Fixture.Create(proxy: "socks5://127.0.0.1:1080");

        var process = fixture.Sut.BuildPreviewAudioProcess("dQw4w9WgXcQ");

        process.StartInfo.ArgumentList.Should().Contain("--proxy");
        process.StartInfo.ArgumentList.Should().Contain("socks5://127.0.0.1:1080");
    }

    [Fact]
    public void OptionSet_includes_embed_thumbnail_and_metadata_for_yt_dlp()
    {
        var options = new OptionSet { ExtractAudio = true, EmbedThumbnail = true, EmbedMetadata = true };

        var args = options.ToString();

        args.Should().Contain("--embed-thumbnail");
        args.Should().Contain("--embed-metadata");
    }
}