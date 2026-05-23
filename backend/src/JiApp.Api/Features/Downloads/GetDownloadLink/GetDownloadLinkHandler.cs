using System;
using System.IO;
using System.Threading.Tasks;
using JiApp.Api.Configuration;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.YtApi;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Downloads.GetDownloadLink;

public sealed class GetDownloadLinkHandler(
    IYoutubeClient youtubeClient,
    ITempFileStore tempFileStore,
    IDownloadHistoryRepository downloadHistoryRepository,
    ICurrentUserService currentUser,
    Settings settings,
    ILogger<GetDownloadLinkHandler> logger)
{
    public const string YoutubeDlErrorCategory = "YoutubeDl";

    public async Task<Result<DownloadResponse>> HandleAsync(DownloadRequest request)
    {
        var baseDirectory = settings.App!.BaseDirectory!;

        // Use userId instead of username to prevent path traversal
        var outputFolder = Path.Combine(baseDirectory, $"YtMp3_{currentUser.UserId}");

        YoutubeClientResponse downloadResult;
        try
        {
            downloadResult = await youtubeClient.DownloadVideoAsync(
                request.VideoId, outputFolder);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.DownloadFailedForVideo(ex, request.VideoId);
            return Result<DownloadResponse>.Failure(
                "Failed to process download. Please try again later.");
        }

        if (!downloadResult.Success)
        {
            var errors = string.Join(", ", downloadResult.Errors);
            logger.YoutubeDlDownloadFailed(request.VideoId, errors);
            return Result<DownloadResponse>.Failure(
                $"Failed to download video: {errors}", errorCategory: YoutubeDlErrorCategory);
        }

        var filePath = downloadResult.FilePath!;
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