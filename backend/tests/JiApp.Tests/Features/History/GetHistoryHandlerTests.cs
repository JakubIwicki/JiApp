using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.History.GetHistory;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Tests.Mocks;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.History;

public class GetHistoryHandlerTests
{
    private readonly Mock<ISearchHistoryRepository> _searchHistoryRepoMock;
    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock;
    private readonly GetHistoryHandler _handler;

    public GetHistoryHandlerTests()
    {
        _searchHistoryRepoMock = SearchHistoryRepositoryMock.GetSuccessful();
        _downloadHistoryRepoMock = DownloadHistoryRepositoryMock.GetSuccessful();
        var currentUserMock = CurrentUserServiceMock.GetSuccessful();
        var loggerMock = LoggerMock.GetSuccessful<GetHistoryHandler>();

        _handler = new GetHistoryHandler(
            _searchHistoryRepoMock.Object,
            _downloadHistoryRepoMock.Object,
            currentUserMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithHistory_ReturnsBothSearchesAndDownloads()
    {
        var searches = new List<YoutubeSearchHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                SearchText = "test query",
                SearchedAt = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc)
            }
        }.AsReadOnly();

        var downloads = new List<YoutubeDownloadHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                VideoTitle = "Test Video",
                VideoId = "abc123",
                VideoUrl = "https://youtube.com/watch?v=abc123",
                DownloadedAt = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc)
            }
        }.AsReadOnly();

        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ReturnsAsync(searches);
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ReturnsAsync(downloads);

        var request = new GetHistoryRequest(null);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Searches.Should().HaveCount(1);
        result.Value.Searches[0].SearchText.Should().Be("test query");
        result.Value.Downloads.Should().HaveCount(1);
        result.Value.Downloads[0].VideoTitle.Should().Be("Test Video");
    }

    [Fact]
    public async Task HandleAsync_WithLimit_RespectsLimitParameter()
    {
        var searches = new List<YoutubeSearchHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                SearchText = "limited result",
                SearchedAt = DateTime.UtcNow
            }
        }.AsReadOnly();

        var downloads = new List<YoutubeDownloadHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                VideoTitle = "Limited Download",
                VideoId = "xyz789",
                DownloadedAt = DateTime.UtcNow
            }
        }.AsReadOnly();

        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 5, 0))
            .ReturnsAsync(searches);
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 5, 0))
            .ReturnsAsync(downloads);

        var request = new GetHistoryRequest(5);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Searches.Should().HaveCount(1);
        result.Value!.Downloads.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyHistory_ReturnsEmptyLists()
    {
        var emptySearches = new List<YoutubeSearchHistory>().AsReadOnly();
        var emptyDownloads = new List<YoutubeDownloadHistory>().AsReadOnly();

        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ReturnsAsync(emptySearches);
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ReturnsAsync(emptyDownloads);

        var request = new GetHistoryRequest(null);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Searches.Should().BeEmpty();
        result.Value!.Downloads.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenSearchHistoryFails_ReturnsPartialDownloadHistory()
    {
        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ReturnsAsync(new List<YoutubeDownloadHistory>
            {
                new()
                {
                    Id = 1,
                    UserId = 1L,
                    VideoTitle = "Partial Video",
                    VideoId = "abc123",
                    DownloadedAt = DateTime.UtcNow
                }
            }.AsReadOnly());

        var request = new GetHistoryRequest(null);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Searches.Should().BeEmpty();
        result.Value.Downloads.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WhenBothRepositoriesFail_ReturnsFailure()
    {
        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var request = new GetHistoryRequest(null);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}