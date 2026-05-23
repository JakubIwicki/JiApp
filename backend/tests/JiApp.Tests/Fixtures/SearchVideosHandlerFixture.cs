using System;
using System.Collections.Generic;
using JiApp.Api.Features.Search.SearchVideos;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Tests.Mocks;
using JiApp.YtApi;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class SearchVideosHandlerFixture
{
    private readonly Mock<IYoutubeClient> _youtubeClientMock = YoutubeClientMock.GetSuccessful();

    private readonly Mock<ISearchHistoryRepository>
        _searchHistoryRepoMock = SearchHistoryRepositoryMock.GetSuccessful();

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = CurrentUserServiceMock.GetSuccessful();

    public SearchVideosHandlerFixture WithSearchVideosAsync(string query, int maxResults,
        IReadOnlyList<YoutubeVideo> result)
    {
        _youtubeClientMock.Setup(x => x.SearchVideosAsync(query, maxResults)).ReturnsAsync(result);
        return this;
    }

    public SearchVideosHandlerFixture WithSearchVideosAsync_Throws(Exception ex)
    {
        _youtubeClientMock.Setup(x => x.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(ex);
        return this;
    }

    public SearchVideosHandlerContext Build()
    {
        var handler = new SearchVideosHandler(
            _youtubeClientMock.Object,
            _searchHistoryRepoMock.Object,
            _currentUserServiceMock.Object,
            LoggerMock.Of<SearchVideosHandler>());

        return new SearchVideosHandlerContext(
            handler, _searchHistoryRepoMock);
    }
}

public sealed record SearchVideosHandlerContext(
    SearchVideosHandler Handler,
    Mock<ISearchHistoryRepository> SearchHistoryRepoMock);