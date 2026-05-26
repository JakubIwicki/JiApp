using JiApp.Api.Features.Search.ArchiveSearch;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Repositories;
using JiApp.Tests.Mocks;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class ArchiveSearchHandlerFixture
{
    private readonly Mock<ISearchHistoryRepository> _searchHistoryRepoMock =
        SearchHistoryRepositoryMock.GetSuccessful();

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = CurrentUserServiceMock.GetSuccessful();

    public ArchiveSearchHandlerFixture WithArchiveAsync(long id, long userId)
    {
        _searchHistoryRepoMock.Setup(x => x.ArchiveAsync(id, userId)).ReturnsAsync(true);
        return this;
    }

    public ArchiveSearchHandlerFixture WithArchiveAsyncNotFound(long id, long userId)
    {
        _searchHistoryRepoMock.Setup(x => x.ArchiveAsync(id, userId)).ReturnsAsync(false);
        return this;
    }

    public ArchiveSearchHandlerContext Build()
    {
        var handler = new ArchiveSearchHandler(
            _searchHistoryRepoMock.Object,
            _currentUserServiceMock.Object);

        return new ArchiveSearchHandlerContext(handler);
    }
}

public sealed record ArchiveSearchHandlerContext(
    ArchiveSearchHandler Handler);