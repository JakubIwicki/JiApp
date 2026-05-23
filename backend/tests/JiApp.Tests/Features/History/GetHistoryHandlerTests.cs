using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.History.GetHistory;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.History;

public class GetHistoryHandlerTests
{
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

        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync(1L, 10, searches, 0)
            .WithDownloadGetByUserIdAsync(1L, 10, downloads, 0)
            .Build();

        var request = new GetHistoryRequest(null);

        var result = await ctx.Handler.HandleAsync(request);

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

        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync(1L, 5, searches, 0)
            .WithDownloadGetByUserIdAsync(1L, 5, downloads, 0)
            .Build();

        var request = new GetHistoryRequest(5);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Searches.Should().HaveCount(1);
        result.Value!.Downloads.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyHistory_ReturnsEmptyLists()
    {
        var emptySearches = new List<YoutubeSearchHistory>().AsReadOnly();
        var emptyDownloads = new List<YoutubeDownloadHistory>().AsReadOnly();

        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync(1L, 10, emptySearches, 0)
            .WithDownloadGetByUserIdAsync(1L, 10, emptyDownloads, 0)
            .Build();

        var request = new GetHistoryRequest(null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Searches.Should().BeEmpty();
        result.Value!.Downloads.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenSearchHistoryFails_ReturnsPartialDownloadHistory()
    {
        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync_Throws(1L, 10, new InvalidOperationException("Database connection failed"), 0)
            .WithDownloadGetByUserIdAsync(1L, 10, new List<YoutubeDownloadHistory>
            {
                new()
                {
                    Id = 1,
                    UserId = 1L,
                    VideoTitle = "Partial Video",
                    VideoId = "abc123",
                    DownloadedAt = DateTime.UtcNow
                }
            }.AsReadOnly(), 0)
            .Build();

        var request = new GetHistoryRequest(null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Searches.Should().BeEmpty();
        result.Value.Downloads.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WhenBothRepositoriesFail_ReturnsFailure()
    {
        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync_Throws(1L, 10, new InvalidOperationException("Database connection failed"), 0)
            .WithDownloadGetByUserIdAsync_Throws(1L, 10, new InvalidOperationException("Database connection failed"), 0)
            .Build();

        var request = new GetHistoryRequest(null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
