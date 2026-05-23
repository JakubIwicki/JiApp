using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Google;
using JiApp.Api.Features.Search.SearchVideos;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Tests.Mocks;
using JiApp.YtApi;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Search;

public class SearchVideosHandlerTests
{
    private readonly Mock<IYoutubeClient> _youtubeClientMock;
    private readonly Mock<ISearchHistoryRepository> _searchHistoryRepoMock;
    private readonly SearchVideosHandler _handler;

    public SearchVideosHandlerTests()
    {
        _youtubeClientMock = YoutubeClientMock.GetSuccessful();
        _searchHistoryRepoMock = SearchHistoryRepositoryMock.GetSuccessful();
        var currentUserMock = CurrentUserServiceMock.GetSuccessful();
        var loggerMock = LoggerMock.GetSuccessful<SearchVideosHandler>();

        _handler = new SearchVideosHandler(
            _youtubeClientMock.Object,
            _searchHistoryRepoMock.Object,
            currentUserMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidQuery_ReturnsResultsAndSavesHistory()
    {
        var videos = new List<YoutubeVideo>
        {
            new("vid1", "Test Title", "Test Description", "https://img.url/1", "Test Channel"),
            new("vid2", "Another Title", "Another Description", "https://img.url/2", "Another Channel")
        }.AsReadOnly();

        _youtubeClientMock.Setup(x => x.SearchVideosAsync("test query", 10))
            .ReturnsAsync(videos);

        var request = new SearchVideosRequest("test query", null);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Results.Should().HaveCount(2);

        var first = result.Value.Results[0];
        first.VideoId.Should().Be("vid1");
        first.Title.Should().Be("Test Title");
        first.VideoUrl.Should().Be("https://www.youtube.com/watch?v=vid1");

        _searchHistoryRepoMock.Verify(x => x.AddAsync(It.Is<YoutubeSearchHistory>(h =>
            h.UserId == 1L &&
            h.SearchText == "test query" &&
            h.SearchedAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMaxResults_ReturnsLimitedResults()
    {
        var videos = new List<YoutubeVideo>
        {
            new YoutubeVideo("vid1", "Title", "Desc", "https://img.url/1", "Channel")
        }.AsReadOnly();

        _youtubeClientMock.Setup(x => x.SearchVideosAsync("query", 5))
            .ReturnsAsync(videos);

        var request = new SearchVideosRequest("query", 5);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResults_ReturnsEmptyList()
    {
        var emptyList = new List<YoutubeVideo>().AsReadOnly();

        _youtubeClientMock.Setup(x => x.SearchVideosAsync("empty query", 10))
            .ReturnsAsync(emptyList);

        var request = new SearchVideosRequest("empty query", null);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenYouTubeApiThrows_ReturnsFailure()
    {
        _youtubeClientMock.Setup(x => x.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new GoogleApiException("youtube", "API error"));

        var request = new SearchVideosRequest("query", null);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
