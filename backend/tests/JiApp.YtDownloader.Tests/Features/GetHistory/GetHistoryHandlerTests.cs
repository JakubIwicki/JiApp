using JiApp.Common.Models;
using JiApp.YtDownloader.Features.GetHistory;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.GetHistory;

public sealed class GetHistoryHandlerTests
{
    private const long UserId = 1L;

    private static readonly DateTime FixedAt = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReturnsCombinedHistory_FromBothRepositories()
    {
        var fixture = new MockFixture()
            .WithSearchReturning(
                CreateSearchEntry("newer", FixedAt.AddHours(1)),
                CreateSearchEntry("older", FixedAt))
            .WithDownloadReturning(CreateDownloadEntry("vid-1"));

        var result = await fixture.Sut.HandleAsync(new GetHistoryRequest(null));

        var response = AssertSuccess(result);
        response.Searches.Select(s => s.SearchText).Should().Equal("newer", "older");
        response.Searches[0].SearchedAt.Should().Be(FixedAt.AddHours(1));
        response.Downloads.Select(d => d.VideoId).Should().Equal("vid-1");
    }

    [Fact]
    public async Task ReturnsFailure_WhenBothRepositoriesThrow()
    {
        var fixture = new MockFixture().WithSearchThrowing().WithDownloadThrowing();

        var result = await fixture.Sut.HandleAsync(new GetHistoryRequest(null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("An error occurred while retrieving history");
        result.ErrorCategory.Should().BeNull();
    }

    [Fact]
    public async Task ReturnsSearchesOnly_WhenDownloadHistoryFails()
    {
        var fixture = new MockFixture()
            .WithSearchReturning(CreateSearchEntry("query", FixedAt))
            .WithDownloadThrowing();

        var result = await fixture.Sut.HandleAsync(new GetHistoryRequest(null));

        var response = AssertSuccess(result);
        response.Searches.Should().ContainSingle();
        response.Downloads.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsDownloadsOnly_WhenSearchHistoryFails()
    {
        var fixture = new MockFixture()
            .WithSearchThrowing()
            .WithDownloadReturning(CreateDownloadEntry("vid-1"));

        var result = await fixture.Sut.HandleAsync(new GetHistoryRequest(null));

        var response = AssertSuccess(result);
        response.Searches.Should().BeEmpty();
        response.Downloads.Should().ContainSingle();
    }

    [Fact]
    public async Task QueriesBothRepositories_WithCurrentUserIdAndRequestedLimit()
    {
        var fixture = new MockFixture().WithSearchReturning().WithDownloadReturning();

        await fixture.Sut.HandleAsync(new GetHistoryRequest(7));

        fixture.SearchRepo.Verify(r => r.GetByUserIdAsync(UserId, 7, 0), Times.Once);
        fixture.DownloadRepo.Verify(r => r.GetByUserIdAsync(UserId, 7, 0), Times.Once);
    }

    [Fact]
    public async Task DefaultsLimit_To10_WhenRequestNull()
    {
        var fixture = new MockFixture().WithSearchReturning().WithDownloadReturning();

        await fixture.Sut.HandleAsync(new GetHistoryRequest(null));

        fixture.SearchRepo.Verify(r => r.GetByUserIdAsync(UserId, 10, 0), Times.Once);
        fixture.DownloadRepo.Verify(r => r.GetByUserIdAsync(UserId, 10, 0), Times.Once);
    }

    private static YoutubeSearchHistory CreateSearchEntry(string searchText, DateTime searchedAt) =>
        new()
        {
            UserId = UserId,
            SearchText = searchText,
            SearchedAt = searchedAt,
        };

    private static YoutubeDownloadHistory CreateDownloadEntry(string videoId) =>
        new()
        {
            UserId = UserId,
            VideoId = videoId,
            VideoTitle = $"Title {videoId}",
            VideoUrl = $"https://youtube.com/watch?v={videoId}",
            DownloadedAt = FixedAt,
        };

    private sealed class MockFixture
    {
        public Mock<ISearchHistoryRepository> SearchRepo { get; } = new();
        public Mock<IDownloadHistoryRepository> DownloadRepo { get; } = new();
        public MockCurrentUserService User { get; } = MockCurrentUserService.GetSuccessful();

        public GetHistoryHandler Sut => new(
            SearchRepo.Object,
            DownloadRepo.Object,
            User.Object,
            Mock.Of<ILogger<GetHistoryHandler>>());

        public MockFixture WithSearchReturning(params YoutubeSearchHistory[] entries)
        {
            SearchRepo
                .Setup(r => r.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(entries);
            return this;
        }

        public MockFixture WithSearchThrowing()
        {
            SearchRepo
                .Setup(r => r.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("search history unavailable"));
            return this;
        }

        public MockFixture WithDownloadReturning(params YoutubeDownloadHistory[] entries)
        {
            DownloadRepo
                .Setup(r => r.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(entries);
            return this;
        }

        public MockFixture WithDownloadThrowing()
        {
            DownloadRepo
                .Setup(r => r.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("download history unavailable"));
            return this;
        }
    }
}
