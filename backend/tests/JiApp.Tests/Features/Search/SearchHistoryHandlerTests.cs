using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Search.SearchHistory;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.Search;

public class SearchHistoryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithHistory_ReturnsUserSearchHistory()
    {
        var history = new List<YoutubeSearchHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                SearchText = "first query",
                SearchedAt = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 2,
                UserId = 1L,
                SearchText = "second query",
                SearchedAt = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc)
            }
        }.AsReadOnly();

        var ctx = new SearchHistoryHandlerFixture()
            .WithGetByUserIdAsync(1L, 10, history, 0)
            .Build();

        var request = new SearchHistoryRequest(null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(2);

        result.Value.Items[0].Id.Should().Be(1);
        result.Value.Items[0].SearchText.Should().Be("first query");

        result.Value.Items[1].Id.Should().Be(2);
        result.Value.Items[1].SearchText.Should().Be("second query");
    }

    [Fact]
    public async Task HandleAsync_WithLimit_ReturnsLimitedHistory()
    {
        var history = new List<YoutubeSearchHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                SearchText = "only one",
                SearchedAt = DateTime.UtcNow
            }
        }.AsReadOnly();

        var ctx = new SearchHistoryHandlerFixture()
            .WithGetByUserIdAsync(1L, 5, history, 0)
            .Build();

        var request = new SearchHistoryRequest(5);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyHistory_ReturnsEmptyList()
    {
        var emptyHistory = new List<YoutubeSearchHistory>().AsReadOnly();

        var ctx = new SearchHistoryHandlerFixture()
            .WithGetByUserIdAsync(1L, 10, emptyHistory, 0)
            .Build();

        var request = new SearchHistoryRequest(null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }
}