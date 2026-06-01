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

    Task<YoutubeClientResponse> DownloadVideoAsync(string videoId, string outputPath,
        CancellationToken cancellationToken = default);

    Task<string> ResolveAudioUrlAsync(string videoId, CancellationToken cancellationToken = default);
}

public sealed class YoutubeClient(
    string apiKey,
    string ytDlpPath,
    string ffmpegPath) : IYoutubeClient, IDisposable
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

        var results = response.Items
            .Where(item => item is { Id.Kind: "youtube#video", Snippet: not null })
            .Select(item => new YoutubeVideo(
                VideoId: item.Id.VideoId,
                Title: WebUtility.HtmlDecode(item.Snippet.Title),
                Description: WebUtility.HtmlDecode(item.Snippet.Description),
                ImageUrl: item.Snippet.Thumbnails.Default__.Url,
                ChannelTitle: WebUtility.HtmlDecode(item.Snippet.ChannelTitle)))
            .ToList();

        return results.AsReadOnly();
    }

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
            Output = outputTemplate
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
        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = $"--no-playlist --extract-audio --audio-format mp3 --get-url \"{videoUrl}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

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