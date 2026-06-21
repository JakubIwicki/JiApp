using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace JiApp.YtApi;

public interface IYoutubeClient
{
    Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10,
        CancellationToken cancellationToken = default);

    Task<YoutubeVideo?> GetVideoByIdAsync(string videoId,
        CancellationToken cancellationToken = default);

    Task<YoutubeClientResponse> DownloadVideoAsync(string videoId, string outputPath,
        CancellationToken cancellationToken = default);

    Task<string> ResolveAudioUrlAsync(string videoId, CancellationToken cancellationToken = default);
}

public sealed class YoutubeClient(
    string apiKey,
    string ytDlpPath,
    string ffmpegPath,
    string? cookiesFile = null,
    string? cookiesFromBrowser = null) : IYoutubeClient, IDisposable
{
    private readonly YouTubeService _youTubeService = new(new Google.Apis.Services.BaseClientService.Initializer
    {
        ApiKey = apiKey
    });

    private readonly YoutubeDL _youtubeDl = new()
    {
        YoutubeDLPath = ytDlpPath,
        FFmpegPath = ffmpegPath,
    };

    private readonly SemaphoreSlim _youtubeDlLock = new(1, 1);

    public async Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = _youTubeService.Search.List("snippet");
        searchRequest.Q = query;
        searchRequest.MaxResults = maxResults;

        var response = await searchRequest.ExecuteAsync(cancellationToken);

        return response.Items
            .Where(item => item is { Id.Kind: "youtube#video", Snippet: not null })
            .Select(MapToYoutubeVideo)
            .ToList()
            .AsReadOnly();
    }

    public async Task<YoutubeVideo?> GetVideoByIdAsync(string videoId,
        CancellationToken cancellationToken = default)
    {
        var listRequest = _youTubeService.Videos.List("snippet");
        listRequest.Id = videoId;
        listRequest.MaxResults = 1;

        var response = await listRequest.ExecuteAsync(cancellationToken);

        return response.Items
            .Where(item => item.Snippet is not null)
            .Select(MapToYoutubeVideo)
            .FirstOrDefault();
    }

    private static YoutubeVideo MapToYoutubeVideo(Google.Apis.YouTube.v3.Data.Video video) =>
        new(
            VideoId: video.Id ?? string.Empty,
            Title: WebUtility.HtmlDecode(video.Snippet?.Title ?? string.Empty),
            Description: WebUtility.HtmlDecode(video.Snippet?.Description ?? string.Empty),
            ImageUrl: video.Snippet?.Thumbnails?.Default__?.Url ?? string.Empty,
            ChannelTitle: WebUtility.HtmlDecode(video.Snippet?.ChannelTitle ?? string.Empty));

    private static YoutubeVideo MapToYoutubeVideo(Google.Apis.YouTube.v3.Data.SearchResult item) =>
        new(
            VideoId: item.Id.VideoId ?? string.Empty,
            Title: WebUtility.HtmlDecode(item.Snippet?.Title ?? string.Empty),
            Description: WebUtility.HtmlDecode(item.Snippet?.Description ?? string.Empty),
            ImageUrl: item.Snippet?.Thumbnails?.Default__?.Url ?? string.Empty,
            ChannelTitle: WebUtility.HtmlDecode(item.Snippet?.ChannelTitle ?? string.Empty));

    public async Task<YoutubeClientResponse> DownloadVideoAsync(string videoId, string outputPath,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputPath);

        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
        var outputTemplate = Path.Combine(outputPath, $"{Guid.NewGuid():N}.%(ext)s");

        var options = new OptionSet
        {
            NoPlaylist = true,
            ExtractAudio = true,
            AudioFormat = AudioConversionFormat.Mp3,
            ExtractorArgs = "youtube:player_client=android_vr",
            Output = outputTemplate,
            // Precedence: cookiesFromBrowser wins over cookiesFile (matching ResolveAudioUrlAsync).
            // When both are set, only pass --cookies-from-browser to avoid conflicting flags.
            CookiesFromBrowser = !string.IsNullOrEmpty(cookiesFromBrowser) ? cookiesFromBrowser : null,
            Cookies = string.IsNullOrEmpty(cookiesFromBrowser) && !string.IsNullOrEmpty(cookiesFile) ? cookiesFile : null,
        };
        await _youtubeDlLock.WaitAsync(cancellationToken);
        try
        {
            var result = await _youtubeDl.RunWithOptions(videoUrl, options, ct: cancellationToken);

            if (!result.Success)
            {
                options.ExtractorArgs = null;
                options.Output = outputTemplate;
                var fallbackResult = await _youtubeDl.RunWithOptions(videoUrl, options, ct: cancellationToken);
                if (fallbackResult.Success)
                    result = fallbackResult;
            }

            if (!result.Success)
                return new YoutubeClientResponse(null, false, result.ErrorOutput ?? []);

            var resolvedPath = result.Data;
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
            {
                resolvedPath = Directory.GetFiles(outputPath, "*.mp3")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }

            return new YoutubeClientResponse(resolvedPath, !string.IsNullOrEmpty(resolvedPath), []);
        }
        finally
        {
            _youtubeDlLock.Release();
        }
    }

    public async Task<string> ResolveAudioUrlAsync(string videoId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(videoId) || !Regex.IsMatch(videoId, @"^[a-zA-Z0-9_-]{11}$"))
            throw new ArgumentException($"Invalid videoId: '{videoId}'", nameof(videoId));

        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";

        // Run yt-dlp directly — YoutubeDLSharp's RunWithOptions doesn't capture
        // stdout when Print = "urls" is used, so result.Data ends up empty.
        var startInfo = new ProcessStartInfo(ytDlpPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("--no-playlist");
        startInfo.ArgumentList.Add("--extract-audio");
        startInfo.ArgumentList.Add("--audio-format");
        startInfo.ArgumentList.Add("mp3");
        if (!string.IsNullOrEmpty(cookiesFromBrowser))
        {
            startInfo.ArgumentList.Add("--cookies-from-browser");
            startInfo.ArgumentList.Add(cookiesFromBrowser);
        }
        else if (!string.IsNullOrEmpty(cookiesFile))
        {
            startInfo.ArgumentList.Add("--cookies");
            startInfo.ArgumentList.Add(cookiesFile);
        }
        startInfo.ArgumentList.Add("--get-url");
        startInfo.ArgumentList.Add(videoUrl);

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("Failed to start yt-dlp process.");

        // Read stdout and stderr concurrently to avoid deadlock when the
        // stderr pipe buffer fills while the parent is blocked on stdout.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync(ct);

        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"""Failed to resolve audio URL for video "{videoId}" (exit code {process.ExitCode}): {stderr.Trim()}""");
        }

        var audioUrl = stdout.Trim();

        if (string.IsNullOrEmpty(audioUrl) || !Uri.TryCreate(audioUrl, UriKind.Absolute, out var audioUri) ||
            audioUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                $"""Resolved audio URL for video "{videoId}" is invalid: '{audioUrl}'""");
        }

        return audioUrl;
    }

    public void Dispose()
    {
        _youTubeService.Dispose();
        _youtubeDlLock.Dispose();
    }
}