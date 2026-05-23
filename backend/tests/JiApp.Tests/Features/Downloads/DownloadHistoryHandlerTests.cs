using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Downloads.DownloadHistory;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class DownloadHistoryHandlerTests
{
    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock;
    private readonly DownloadHistoryHandler _handler;

    public DownloadHistoryHandlerTests()
    {
        _downloadHistoryRepoMock = DownloadHistoryRepositoryMock.GetSuccessful();
        var currentUserMock = CurrentUserServiceMock.GetSuccessful();
        var loggerMock = LoggerMock.GetSuccessful<DownloadHistoryHandler>();

        _handler = new DownloadHistoryHandler(
            _downloadHistoryRepoMock.Object,
            currentUserMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithHistory_ReturnsUserDownloadHistory()
    {
        var history = new List<YoutubeDownloadHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                VideoTitle = "test video one",
                VideoId = "abc123",
                VideoUrl = "https://youtube.com/watch?v=abc123",
                DownloadedAt = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 2,
                UserId = 1L,
                VideoTitle = "test video two",
                VideoId = "def456",
                VideoUrl = "https://youtube.com/watch?v=def456",
                DownloadedAt = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc)
            }
        }.AsReadOnly();

        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ReturnsAsync(history);

        var request = new DownloadHistoryRequest(null);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(2);

        result.Value.Items[0].Id.Should().Be(1);
        result.Value.Items[0].VideoTitle.Should().Be("test video one");
        result.Value.Items[0].VideoId.Should().Be("abc123");
        result.Value.Items[0].VideoUrl.Should().Be("https://youtube.com/watch?v=abc123");

        result.Value.Items[1].Id.Should().Be(2);
        result.Value.Items[1].VideoTitle.Should().Be("test video two");
        result.Value.Items[1].VideoId.Should().Be("def456");
        result.Value.Items[1].VideoUrl.Should().Be("https://youtube.com/watch?v=def456");
    }

    [Fact]
    public async Task HandleAsync_WithLimit_ReturnsLimitedHistory()
    {
        var history = new List<YoutubeDownloadHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                VideoTitle = "only one",
                VideoId = "xyz789",
                VideoUrl = "https://youtube.com/watch?v=xyz789",
                DownloadedAt = DateTime.UtcNow
            }
        }.AsReadOnly();

        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 5, 0))
            .ReturnsAsync(history);

        var request = new DownloadHistoryRequest(5);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyHistory_ReturnsEmptyList()
    {
        var emptyHistory = new List<YoutubeDownloadHistory>().AsReadOnly();

        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(1L, 10, 0))
            .ReturnsAsync(emptyHistory);

        var request = new DownloadHistoryRequest(null);

        var result = await _handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }
}
