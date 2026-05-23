using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Google;
using JiApp.Api.Features.Search.SearchVideos;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using JiApp.YtApi;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Search;

public class SearchVideosHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidQuery_ReturnsResultsAndSavesHistory()
    {
        var videos = new List<YoutubeVideo>
        {
            new("vid1", "Test Title", "Test Description", "https://img.url/1", "Test Channel"),
            new("vid2", "Another Title", "Another Description", "https://img.url/2", "Another Channel")
        }.AsReadOnly();

        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("test query", 10, videos)
            .Build();

        var request = new SearchVideosRequest("test query", null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Results.Should().HaveCount(2);

        var first = result.Value.Results[0];
        first.VideoId.Should().Be("vid1");
        first.Title.Should().Be("Test Title");
        first.VideoUrl.Should().Be("https://www.youtube.com/watch?v=vid1");

        ctx.SearchHistoryRepoMock.Verify(x => x.AddAsync(It.Is<YoutubeSearchHistory>(h =>
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

        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("query", 5, videos)
            .Build();

        var request = new SearchVideosRequest("query", 5);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResults_ReturnsEmptyList()
    {
        var emptyList = new List<YoutubeVideo>().AsReadOnly();

        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("empty query", 10, emptyList)
            .Build();

        var request = new SearchVideosRequest("empty query", null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenYouTubeApiThrows_ReturnsFailure()
    {
        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync_Throws(new GoogleApiException("youtube", "API error"))
            .Build();

        var request = new SearchVideosRequest("query", null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
