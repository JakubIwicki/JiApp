using System.Collections.Generic;
using JiApp.Api.Features.Search.SearchHistory;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Tests.Mocks;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class SearchHistoryHandlerFixture
{
    private readonly Mock<ISearchHistoryRepository>
        _searchHistoryRepoMock = SearchHistoryRepositoryMock.GetSuccessful();

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = CurrentUserServiceMock.GetSuccessful();

    public SearchHistoryHandlerFixture WithGetByUserIdAsync(long userId, int limit,
        IReadOnlyList<YoutubeSearchHistory> result, int offset = 0)
    {
        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ReturnsAsync(result);
        return this;
    }

    public SearchHistoryHandlerContext Build()
    {
        var handler = new SearchHistoryHandler(
            _searchHistoryRepoMock.Object,
            _currentUserServiceMock.Object,
            LoggerMock.Of<SearchHistoryHandler>());

        return new SearchHistoryHandlerContext(handler);
    }
}

public sealed record SearchHistoryHandlerContext(
    SearchHistoryHandler Handler);