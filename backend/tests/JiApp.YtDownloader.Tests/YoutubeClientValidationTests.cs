using JiApp.YtApi;

namespace JiApp.YtDownloader.Tests;

public class YoutubeClientValidationTests
{
    private static YoutubeClient CreateClient(
        string? cookiesFile = null, string? cookiesFromBrowser = null) =>
        new("fake-key", "yt-dlp", "ffmpeg", cookiesFile, cookiesFromBrowser);

    [Theory]
    [InlineData("")]
    [InlineData("abc")] // too short
    [InlineData("abcdefghijklm")] // too long (13)
    [InlineData("abc-def-abc-")] // wrong length (12, but ok pattern)
    [InlineData("abc def ghijk")] // contains space
    [InlineData("<script>")] // XSS payload
    [InlineData("../../etc/passwd")] // path traversal
    [InlineData("--exec=rm -rf /")] // argument injection
    public async Task ResolveAudioUrlAsync_throws_ArgumentException_for_invalid_videoId(string invalidVideoId)
    {
        var client = CreateClient();

        var act = async () => await client.ResolveAudioUrlAsync(invalidVideoId);

        (await act.Should().ThrowExactlyAsync<ArgumentException>())
            .And.Message.Should().Contain("videoId");
    }

    [Theory]
    [InlineData("dQw4w9WgXcQ")] // 11 chars, standard
    public async Task ResolveAudioUrlAsync_does_not_throw_for_plausible_videoId(string plausibleVideoId)
    {
        var client = CreateClient();

        // Note: this will still fail because yt-dlp isn't installed, but it should
        // NOT be an ArgumentException — the validation guard must pass it through.
        var act = async () => await client.ResolveAudioUrlAsync(plausibleVideoId);

        // The validation guard must pass valid IDs through — no ArgumentException expected.
        try
        {
            await client.ResolveAudioUrlAsync(plausibleVideoId);
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
    public async Task ResolveAudioUrlAsync_passes_cookies_from_browser_to_yt_dlp()
    {
        var client = CreateClient(cookiesFromBrowser: "madeupbrowser");

        var act = async () => await client.ResolveAudioUrlAsync("dQw4w9WgXcQ");

        (await act.Should().ThrowExactlyAsync<InvalidOperationException>())
            .And.Message.Should().Contain("unsupported browser");
    }

    [Fact]
    public async Task ResolveAudioUrlAsync_passes_cookies_file_to_yt_dlp()
    {
        var client = CreateClient(cookiesFile: "/nonexistent/cookies.txt");

        var act = async () => await client.ResolveAudioUrlAsync("dQw4w9WgXcQ");

        var ex = await act.Should().ThrowExactlyAsync<InvalidOperationException>();
        ex.And.Message.Should().Contain("cookies");
    }

    [Fact]
    public async Task ResolveAudioUrlAsync_cookies_from_browser_takes_precedence_over_file()
    {
        var client = CreateClient(
            cookiesFile: "/nonexistent/cookies.txt",
            cookiesFromBrowser: "madeupbrowser");

        var act = async () => await client.ResolveAudioUrlAsync("dQw4w9WgXcQ");

        // When both are set, cookiesFromBrowser wins → expect "unsupported browser" error,
        // NOT the "FileNotFoundError" from trying to use the file path.
        (await act.Should().ThrowExactlyAsync<InvalidOperationException>())
            .And.Message.Should().Contain("unsupported browser");
    }
}