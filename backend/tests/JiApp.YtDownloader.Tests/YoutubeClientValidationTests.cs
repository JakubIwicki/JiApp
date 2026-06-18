using JiApp.YtApi;

namespace JiApp.YtDownloader.Tests;

public sealed class YoutubeClientValidationTests
{
    private sealed class Fixture
    {
        public YoutubeClient Sut { get; }

        public Fixture(string? cookiesFile = null, string? cookiesFromBrowser = null)
        {
            Sut = new YoutubeClient("fake-key", "yt-dlp", "ffmpeg", cookiesFile, cookiesFromBrowser);
        }

        public static Fixture Create(string? cookiesFile = null, string? cookiesFromBrowser = null) =>
            new(cookiesFile, cookiesFromBrowser);
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
    public async Task ResolveAudioUrlAsync_ThrowsArgumentException_ForInvalidVideoId(string invalidVideoId)
    {
        var fixture = Fixture.Create();

        var act = async () => await fixture.Sut.ResolveAudioUrlAsync(invalidVideoId);

        (await act.Should().ThrowExactlyAsync<ArgumentException>())
            .And.Message.Should().Contain("videoId");
    }

    [Theory]
    [InlineData("dQw4w9WgXcQ")] // 11 chars, standard
    public async Task ResolveAudioUrlAsync_DoesNotThrow_ForPlausibleVideoId(string plausibleVideoId)
    {
        var fixture = Fixture.Create();

        // This may fail (network / yt-dlp error), but it must NOT be an
        // ArgumentException — the validation guard must pass valid IDs through.
        try
        {
            await fixture.Sut.ResolveAudioUrlAsync(plausibleVideoId);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception)
        {
            // Any non-ArgumentException exception is fine — it means yt-dlp
            // was called, which is the correct behavior after validation passes.
        }
    }

    [Fact]
    public async Task ResolveAudioUrlAsync_PassesCookiesFromBrowser_ToYtDlp()
    {
        var fixture = Fixture.Create(cookiesFromBrowser: "madeupbrowser");

        var act = async () => await fixture.Sut.ResolveAudioUrlAsync("dQw4w9WgXcQ");

        (await act.Should().ThrowExactlyAsync<InvalidOperationException>())
            .And.Message.Should().Contain("unsupported browser");
    }

    [Fact]
    public async Task ResolveAudioUrlAsync_PassesCookiesFile_ToYtDlp()
    {
        // Use a directory as the cookies "file" — yt-dlp fails immediately
        // (before any network call) with "Is a directory", which proves the
        // --cookies flag was passed with the given path.
        var fixture = Fixture.Create(cookiesFile: "/tmp");

        var act = async () => await fixture.Sut.ResolveAudioUrlAsync("dQw4w9WgXcQ");

        var ex = await act.Should().ThrowExactlyAsync<InvalidOperationException>();
        ex.And.Message.Should().Contain("Is a directory");
    }

    [Fact]
    public async Task ResolveAudioUrlAsync_CookiesFromBrowser_TakesPrecedenceOverFile()
    {
        var fixture = Fixture.Create(
            cookiesFile: "/tmp",
            cookiesFromBrowser: "madeupbrowser");

        var act = async () => await fixture.Sut.ResolveAudioUrlAsync("dQw4w9WgXcQ");

        // When both are set, cookiesFromBrowser wins → expect "unsupported browser" error,
        // NOT the "Is a directory" error from trying to use the file path.
        (await act.Should().ThrowExactlyAsync<InvalidOperationException>())
            .And.Message.Should().Contain("unsupported browser");
    }
}