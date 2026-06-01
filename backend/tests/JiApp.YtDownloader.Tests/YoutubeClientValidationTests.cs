using JiApp.YtApi;

namespace JiApp.YtDownloader.Tests;

public class YoutubeClientValidationTests
{
    private static YoutubeClient CreateClient() => new("fake-key", "yt-dlp", "ffmpeg");

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
}