using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Repositories;

namespace JiApp.YtDownloader.Features.ArchiveSearch;

public sealed class ArchiveSearchHandler(
    ISearchHistoryRepository searchHistoryRepository,
    ICurrentUserService currentUser)
{
    public async Task<Result<bool>> HandleAsync(ArchiveSearchRequest request)
    {
        var archived = await searchHistoryRepository.ArchiveAsync(request.Id, currentUser.UserId);

        return archived
            ? Result<bool>.Success(true)
            : Result<bool>.Failure("Search history entry not found or does not belong to the current user");
    }
}
