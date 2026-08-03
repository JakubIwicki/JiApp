using System.Threading.Channels;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Services;

namespace JiApp.YtDownloader.Features.GetDownloadLink;

public sealed class GetDownloadLinkHandler(
    IDownloadJobStore jobStore,
    Channel<string> downloadQueue,
    ICurrentUserService currentUser)
{
    public Task<Result<DownloadResponse>> HandleAsync(DownloadRequest request)
    {
        var tempId = jobStore.CreateJob(
            currentUser.UserId,
            request.VideoId,
            request.Title ?? string.Empty,
            request.Description,
            request.ImageUrl,
            request.VideoUrl);

        downloadQueue.Writer.TryWrite(tempId);

        return Task.FromResult(Result<DownloadResponse>.Success(new DownloadResponse(tempId, string.Empty)));
    }
}
