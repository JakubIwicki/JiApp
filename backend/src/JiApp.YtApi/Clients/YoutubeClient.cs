using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;
using JiApp.Common.Resilience;
using JiApp.YtApi.Contracts;
using Polly;

namespace JiApp.YtApi.Clients;

public interface IYoutubeClient
{
    Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10,
        CancellationToken cancellationToken = default);

    Task<YoutubeVideo?> GetVideoByIdAsync(string videoId,
        CancellationToken cancellationToken = default);

    Task<YoutubeClientResponse> DownloadVideoAsync(string videoId, string outputPath, string tempId,
        CancellationToken cancellationToken = default);

    Process BuildPreviewAudioProcess(string videoId);
}

public sealed class YoutubeClient(
    string apiKey,
    string ytDlpPath,
    string ffmpegPath,
    string? cookiesFile = null,
    string? cookiesFromBrowser = null,
    string? proxy = null,
    Google.Apis.Http.IHttpClientFactory? httpClientFactory = null,
    IRetryPolicyFactory? retryPolicyFactory = null) : IYoutubeClient, IDisposable
{
    private readonly YouTubeService _youTubeService = CreateYouTubeService(apiKey, httpClientFactory);

    private readonly ResiliencePipeline _retryPipeline =
        retryPolicyFactory?.RetryOnTransientHttp_WithExponentialBackoff(shouldRetry: ShouldRetry)
        ?? new ResiliencePipelineBuilder().Build();

    public async Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = _youTubeService.Search.List("snippet");
        searchRequest.Q = query;
        searchRequest.MaxResults = maxResults;

        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            Google.Apis.YouTube.v3.Data.SearchListResponse response;
            try
            {
                response = await searchRequest.ExecuteAsync(ct);
            }
            catch (Google.GoogleApiException ex)
            {
                throw new YoutubeApiException("YouTube API request failed.", ex);
            }

            return (response.Items ?? [])
                .Where(item => item is { Id.Kind: "youtube#video", Snippet: not null })
                .Select(MapToYoutubeVideo)
                .ToList()
                .AsReadOnly();
        }, cancellationToken);
    }

    public async Task<YoutubeVideo?> GetVideoByIdAsync(string videoId,
        CancellationToken cancellationToken = default)
    {
        var listRequest = _youTubeService.Videos.List("snippet");
        listRequest.Id = videoId;
        listRequest.MaxResults = 1;

        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            Google.Apis.YouTube.v3.Data.VideoListResponse response;
            try
            {
                response = await listRequest.ExecuteAsync(ct);
            }
            catch (Google.GoogleApiException ex)
            {
                throw new YoutubeApiException("YouTube API request failed.", ex);
            }

            return (response.Items ?? [])
                .Where(item => item.Snippet is not null)
                .Select(MapToYoutubeVideo)
                .FirstOrDefault();
        }, cancellationToken);
    }

    /// <summary>
    /// Retries quota/rate-limit and server errors: the owned <see cref="YoutubeApiException"/>
    /// wraps the inner Google API exception carrying the HTTP status. A 403 quota error is a
    /// deliberate non-transient signal and passes straight through.
    /// </summary>
    private static bool ShouldRetry(Exception exception) =>
        exception is YoutubeApiException { InnerException: Google.GoogleApiException google }
        && google.HttpStatusCode is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError;

    private static YouTubeService CreateYouTubeService(string apiKey, Google.Apis.Http.IHttpClientFactory? httpClientFactory)
    {
        var initializer = new Google.Apis.Services.BaseClientService.Initializer { ApiKey = apiKey };
        if (httpClientFactory is not null)
            initializer.HttpClientFactory = httpClientFactory;
        return new YouTubeService(initializer);
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

    public async Task<YoutubeClientResponse> DownloadVideoAsync(string videoId, string outputPath, string tempId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateVideoId(videoId);
        }
        catch (ArgumentException)
        {
            return new YoutubeClientResponse(null, false, ["Invalid video id."]);
        }

        Directory.CreateDirectory(outputPath);

        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
        // Keyed to the job's temp id so the resulting file path is deterministic
        // per job — a fallback never globs the newest *.mp3 in a shared user folder,
        // which two concurrent downloads could cross-resolve.
        var outputTemplate = Path.Combine(outputPath, $"{tempId}.%(ext)s");

        // Attempt 1 carries the tv-client override; a non-zero exit (bot-check, client
        // rejected) falls back without the override so the config-file POT args from
        // /etc/yt-dlp.conf stay active — never android_vr, which 403s media URLs.
        var result = await RunYtDlpAsync(
            videoUrl, BuildDownloadArgs(outputTemplate, includeTvClientExtractorArgs: true), cancellationToken);

        if (!result.Success)
        {
            var fallback = await RunYtDlpAsync(
                videoUrl, BuildDownloadArgs(outputTemplate, includeTvClientExtractorArgs: false), cancellationToken);
            if (fallback.Success)
                result = fallback;
        }

        if (!result.Success)
            return new YoutubeClientResponse(null, false, result.Errors);

        var resolvedPath = ResolveOutputFilePath(outputPath, tempId, ytDlpReportedPath: null);

        return new YoutubeClientResponse(resolvedPath, !string.IsNullOrEmpty(resolvedPath), []);
    }

    internal List<string> BuildDownloadArgs(string outputTemplate, bool includeTvClientExtractorArgs)
    {
        var args = new List<string>
        {
            "--no-playlist",
            "-x",
            "--audio-format",
            "mp3",
            "--embed-thumbnail",
            "--embed-metadata",
            // yt-dlp-side bounds: a hung network read or endless retry loop reaps itself
            // instead of pinning the worker slot until the .NET-side deadline.
            "--retries",
            "2",
            "--fragment-retries",
            "2",
            "--socket-timeout",
            "15",
            "--max-filesize",
            "500M",
        };
        if (includeTvClientExtractorArgs)
        {
            // android_vr media URLs 403 on YouTube (same issue documented in BuildPreviewAudioProcess);
            // tv is the current stable client. Re-check when downloads regress.
            args.Add("--extractor-args");
            args.Add("youtube:player_client=tv");
        }
        // Precedence: cookiesFromBrowser wins over cookiesFile.
        // When both are set, only pass --cookies-from-browser to avoid conflicting flags.
        if (!string.IsNullOrEmpty(cookiesFromBrowser))
        {
            args.Add("--cookies-from-browser");
            args.Add(cookiesFromBrowser);
        }
        else if (!string.IsNullOrEmpty(cookiesFile))
        {
            args.Add("--cookies");
            args.Add(cookiesFile);
        }
        if (!string.IsNullOrEmpty(proxy))
        {
            args.Add("--proxy");
            args.Add(proxy);
        }
        // A bare name (e.g. the default "ffmpeg") resolves from PATH like the streaming
        // preview relies on; only an explicit path needs --ffmpeg-location.
        if (!string.IsNullOrEmpty(ffmpegPath)
            && (Path.IsPathRooted(ffmpegPath) || ffmpegPath.Contains(Path.DirectorySeparatorChar)))
        {
            args.Add("--ffmpeg-location");
            args.Add(ffmpegPath);
        }
        args.Add("-o");
        args.Add(outputTemplate);
        return args;
    }

    /// <summary>
    /// Spawns yt-dlp directly rather than through YoutubeDLSharp: owning the <see cref="Process"/>
    /// means a deadline can hard-kill the whole tree (yt-dlp plus any ffmpeg child), which the
    /// library's cancellation could not — a stuck child left its job Processing forever. Stdout
    /// and stderr are drained concurrently so a chatty child cannot fill a pipe buffer and
    /// deadlock the wait.
    /// </summary>
    private async Task<YtDlpRunResult> RunYtDlpAsync(string videoUrl, IReadOnlyList<string> args, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo(ytDlpPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);
        startInfo.ArgumentList.Add(videoUrl);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            KillProcessTree(process);
            try
            {
                // After the kill the pipes close; bound the drain in case the child survived
                // the kill. Either way the cancellation propagates so the worker's deadline
                // path runs.
                await Task.WhenAll(stdoutTask, stderrTask).WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
            }
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return process.ExitCode == 0
            ? new YtDlpRunResult(true, [])
            : new YtDlpRunResult(false, ParseYtDlpErrors(stderr));
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // The process exited (or cannot be reaped on this platform) between the cancel
            // and the kill — nothing left to kill.
        }
    }

    private static string[] ParseYtDlpErrors(string stderr)
    {
        var lines = stderr
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .TakeLast(20)
            .ToArray();

        return lines.Length == 0 ? ["yt-dlp exited with a non-zero exit code."] : lines;
    }

    private sealed record YtDlpRunResult(bool Success, string[] Errors);

    private static void ValidateVideoId(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId) || !Regex.IsMatch(videoId, @"^[a-zA-Z0-9_-]{11}$"))
            throw new ArgumentException($"Invalid videoId: '{videoId}'", nameof(videoId));
    }

    /// <summary>
    /// yt-dlp sometimes reports an empty output path. The fallback is keyed to the
    /// job's temp id (matching the output template) rather than globbing the newest
    /// *.mp3 in the user's folder, which two concurrent downloads could cross-resolve.
    /// </summary>
    internal static string? ResolveOutputFilePath(string outputPath, string tempId, string? ytDlpReportedPath)
    {
        if (!string.IsNullOrEmpty(ytDlpReportedPath) && File.Exists(ytDlpReportedPath))
            return ytDlpReportedPath;

        var deterministicPath = Path.Combine(outputPath, $"{tempId}.mp3");
        return File.Exists(deterministicPath) ? deterministicPath : null;
    }

    public Process BuildPreviewAudioProcess(string videoId)
    {
        ValidateVideoId(videoId);

        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";

        var startInfo = new ProcessStartInfo(ytDlpPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("--no-playlist");
        // No --extractor-args override: the default (web) client picks up the PO token
        // from /etc/yt-dlp.conf and streams through WARP, whereas android_vr produces
        // format URLs that 403 even through the proxy with no fallback in the single-shot
        // streaming preview.
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("bestaudio[ext=webm]/bestaudio");
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
        if (!string.IsNullOrEmpty(proxy))
        {
            startInfo.ArgumentList.Add("--proxy");
            startInfo.ArgumentList.Add(proxy);
        }
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("-");
        startInfo.ArgumentList.Add(videoUrl);

        return new Process { StartInfo = startInfo };
    }

    public void Dispose()
    {
        _youTubeService.Dispose();
    }
}