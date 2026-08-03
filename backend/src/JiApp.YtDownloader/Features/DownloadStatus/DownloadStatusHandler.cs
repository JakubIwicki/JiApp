using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Services;

namespace JiApp.YtDownloader.Features.DownloadStatus;

public sealed class DownloadStatusHandler(
    IDownloadJobStore jobStore,
    ICurrentUserService currentUser)
{
    public Result<DownloadStatusResponse> Handle(string id)
    {
        var status = jobStore.GetStatus(id, currentUser.UserId);

        if (status is null)
            return Result<DownloadStatusResponse>.Failure("Download job not found.", ResultCategories.NotFound);

        var state = status.Status switch
        {
            DownloadJobStatus.Running => "running",
            DownloadJobStatus.Ready => "ready",
            DownloadJobStatus.Failed => "failed",
            _ => "pending"
        };

        return Result<DownloadStatusResponse>.Success(
            new DownloadStatusResponse(state, status.Error, status.ErrorCategory));
    }
}
