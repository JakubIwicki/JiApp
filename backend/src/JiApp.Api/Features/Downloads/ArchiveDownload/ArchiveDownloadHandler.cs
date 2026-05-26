using System.Threading.Tasks;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Repositories;

namespace JiApp.Api.Features.Downloads.ArchiveDownload;

public sealed class ArchiveDownloadHandler(
    IDownloadHistoryRepository downloadHistoryRepository,
    ICurrentUserService currentUser)
{
    public async Task<Result<bool>> HandleAsync(ArchiveDownloadRequest request)
    {
        var archived = await downloadHistoryRepository.ArchiveAsync(request.Id, currentUser.UserId);

        if (!archived)
            return Result<bool>.Failure("Download history entry not found or does not belong to the current user");

        return Result<bool>.Success(true);
    }
}