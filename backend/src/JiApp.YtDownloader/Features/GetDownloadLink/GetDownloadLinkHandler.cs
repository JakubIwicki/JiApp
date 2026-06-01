using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Repositories;
using JiApp.YtDownloader.Services;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.GetDownloadLink;

public sealed class GetDownloadLinkHandler(
    IYoutubeClient youtubeClient,
    ITempFileStore tempFileStore,
    IDownloadHistoryRepository downloadHistoryRepository,
    ICurrentUserService currentUser,
    Settings settings,
    ILogger<GetDownloadLinkHandler> logger)
{
    public const string YoutubeDlErrorCategory = "YoutubeDl";

    private static readonly SemaphoreSlim DownloadSemaphore = new(
        initialCount: 3, maxCount: 3);

    public async Task<Result<DownloadResponse>> HandleAsync(DownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseDirectory = settings.App?.BaseDirectory ?? "/tmp";

        var outputFolder = Path.Combine(baseDirectory, $"YtMp3_{currentUser.UserId}");

        YoutubeClientResponse downloadResult;
        await DownloadSemaphore.WaitAsync(cancellationToken);
        try
        {
            downloadResult = await youtubeClient.DownloadVideoAsync(
                request.VideoId, outputFolder, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.DownloadFailedForVideo(ex, request.VideoId);
            return Result<DownloadResponse>.Failure(
                "Failed to process download. Please try again later.");
        }
        finally
        {
            DownloadSemaphore.Release();
        }

        if (!downloadResult.Success)
        {
            var errors = string.Join(", ", downloadResult.Errors);
            logger.YoutubeDlDownloadFailed(request.VideoId, errors);
            return Result<DownloadResponse>.Failure(
                "Failed to download video. Please try again later.", errorCategory: YoutubeDlErrorCategory);
        }

        var filePath = downloadResult.FilePath ??
                       throw new InvalidOperationException("Download result FilePath is null despite Success=true");

        if (!File.Exists(filePath))
        {
            return Result<DownloadResponse>.Failure("Download completed but file is missing.",
                errorCategory: YoutubeDlErrorCategory);
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            File.Delete(filePath);
            return Result<DownloadResponse>.Failure("Download completed but file is empty.",
                errorCategory: YoutubeDlErrorCategory);
        }

        var tempId = tempFileStore.Add(filePath, currentUser.UserId);

        var historyEntry = new YoutubeDownloadHistory
        {
            UserId = currentUser.UserId,
            DownloadedAt = DateTime.UtcNow,
            VideoTitle = request.Title,
            VideoDescription = request.Description,
            VideoId = request.VideoId,
            VideoUrl = request.VideoUrl,
            ImageUrl = request.ImageUrl
        };

        await downloadHistoryRepository.AddAsync(historyEntry);
        await downloadHistoryRepository.SaveChangesAsync();

        return Result<DownloadResponse>.Success(new DownloadResponse(tempId, string.Empty));
    }
}
