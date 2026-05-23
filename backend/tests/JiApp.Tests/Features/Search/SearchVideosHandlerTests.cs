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
            .WithSearchVideosAsync("test query", 50, videos)
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
            new("vid1", "Title", "Desc", "https://img.url/1", "Channel"),
            new("vid2", "Title2", "Desc2", "https://img.url/2", "Channel2"),
            new("vid3", "Title3", "Desc3", "https://img.url/3", "Channel3"),
            new("vid4", "Title4", "Desc4", "https://img.url/4", "Channel4"),
            new("vid5", "Title5", "Desc5", "https://img.url/5", "Channel5"),
            new("vid6", "Title6", "Desc6", "https://img.url/6", "Channel6")
        }.AsReadOnly();

        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("query", 50, videos)
            .Build();

        var request = new SearchVideosRequest("query", 5);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(5);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResults_ReturnsEmptyList()
    {
        var emptyList = new List<YoutubeVideo>().AsReadOnly();

        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("empty query", 50, emptyList)
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

    [Fact]
    public async Task HandleAsync_WithCachedQuery_SkipsYouTubeApiOnSecondCall()
    {
        var videos = new List<YoutubeVideo>
        {
            new("vid1", "Cached Title", "Cached Desc", "https://img.url/1", "Cached Channel")
        }.AsReadOnly();

        var fixture = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("cached query", 50, videos);

        var ctx = fixture.Build();

        var request = new SearchVideosRequest("cached query", null);

        // First call — populates cache via YouTube API
        var first = await ctx.Handler.HandleAsync(request);
        first.IsSuccess.Should().BeTrue();

        // Second call — must hit cache, NOT YouTube API
        var second = await ctx.Handler.HandleAsync(request);
        second.IsSuccess.Should().BeTrue();
        second.Value!.Results.Should().HaveCount(1);
        second.Value.Results[0].Title.Should().Be("Cached Title");

        // Verify YouTube API was called exactly once (first call only)
        fixture.YoutubeClientMock.Verify(
            x => x.SearchVideosAsync("cached query", 50),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDifferentCasing_UsesSameCacheKey()
    {
        var videos = new List<YoutubeVideo>
        {
            new("vid1", "Title", "Desc", "https://img.url/1", "Channel")
        }.AsReadOnly();

        var fixture = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("Lo-Fi Beats", 50, videos);

        var ctx = fixture.Build();

        // First call with original casing
        var first = await ctx.Handler.HandleAsync(new SearchVideosRequest("Lo-Fi Beats", null));
        first.IsSuccess.Should().BeTrue();

        // Second call with different casing and whitespace — must hit cache
        var second = await ctx.Handler.HandleAsync(new SearchVideosRequest("  lo-fi beats  ", null));
        second.IsSuccess.Should().BeTrue();
        second.Value!.Results.Should().HaveCount(1);

        // API called only once
        fixture.YoutubeClientMock.Verify(
            x => x.SearchVideosAsync("Lo-Fi Beats", 50),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDifferentQuery_CallsApiForEach()
    {
        var rapVideos = new List<YoutubeVideo>
        {
            new("r1", "Rap Title", "Rap Desc", "https://img.url/r1", "Rap Channel")
        }.AsReadOnly();

        var jazzVideos = new List<YoutubeVideo>
        {
            new("j1", "Jazz Title", "Jazz Desc", "https://img.url/j1", "Jazz Channel")
        }.AsReadOnly();

        var fixture = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("rap", 50, rapVideos)
            .WithSearchVideosAsync("jazz", 50, jazzVideos);

        var ctx = fixture.Build();

        var first = await ctx.Handler.HandleAsync(new SearchVideosRequest("rap", null));
        first.IsSuccess.Should().BeTrue();
        first.Value!.Results[0].Title.Should().Be("Rap Title");

        var second = await ctx.Handler.HandleAsync(new SearchVideosRequest("jazz", null));
        second.IsSuccess.Should().BeTrue();
        second.Value!.Results[0].Title.Should().Be("Jazz Title");

        // Each query called API exactly once
        fixture.YoutubeClientMock.Verify(
            x => x.SearchVideosAsync("rap", 50), Times.Once);
        fixture.YoutubeClientMock.Verify(
            x => x.SearchVideosAsync("jazz", 50), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMaxResultsLessThanCached_SlicesCorrectly()
    {
        var videos = new List<YoutubeVideo>();
        for (int i = 1; i <= 50; i++)
            videos.Add(new($"vid{i}", $"Title {i}", $"Desc {i}", $"https://img.url/{i}", $"Channel {i}"));

        var fixture = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("bulk", 50, videos.AsReadOnly());

        var ctx = fixture.Build();

        var result = await ctx.Handler.HandleAsync(new SearchVideosRequest("bulk", 3));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(3);
        result.Value.Results[0].VideoId.Should().Be("vid1");
        result.Value.Results[2].VideoId.Should().Be("vid3");
    }
}