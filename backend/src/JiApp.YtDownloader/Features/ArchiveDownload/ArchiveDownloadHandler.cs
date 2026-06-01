using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Repositories;

namespace JiApp.YtDownloader.Features.ArchiveDownload;

public sealed class ArchiveDownloadHandler(
    IDownloadHistoryRepository downloadHistoryRepository,
    ICurrentUserService currentUser)
{
    public async Task<Result<bool>> HandleAsync(ArchiveDownloadRequest request)
    {
        var archived = await downloadHistoryRepository.ArchiveAsync(request.Id, currentUser.UserId);

        return archived
            ? Result<bool>.Success(true)
            : Result<bool>.Failure("Download history entry not found or does not belong to the current user");
    }
}