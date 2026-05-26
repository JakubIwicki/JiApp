using System.Threading.Tasks;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Repositories;

namespace JiApp.Api.Features.Search.ArchiveSearch;

public sealed class ArchiveSearchHandler(
    ISearchHistoryRepository searchHistoryRepository,
    ICurrentUserService currentUser)
{
    public async Task<Result<bool>> HandleAsync(ArchiveSearchRequest request)
    {
        var archived = await searchHistoryRepository.ArchiveAsync(request.Id, currentUser.UserId);

        if (!archived)
            return Result<bool>.Failure("Search history entry not found or does not belong to the current user");

        return Result<bool>.Success(true);
    }
}