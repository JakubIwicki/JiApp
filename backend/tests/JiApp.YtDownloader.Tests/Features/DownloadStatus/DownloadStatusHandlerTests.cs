using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Features.DownloadStatus;
using JiApp.YtDownloader.Services;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.DownloadStatus;

public sealed class DownloadStatusHandlerTests
{
    private const long UserId = 42L;
    private const long OtherUserId = 43L;
    private const string VideoId = "dQw4w9WgXcQ";

    private sealed class Fixture
    {
        public DownloadJobStore JobStore { get; } = new(TimeSpan.FromMinutes(15));
        public DownloadStatusHandler Sut { get; }

        public Fixture()
        {
            var user = Mock.Of<ICurrentUserService>(x => x.UserId == UserId);
            Sut = new DownloadStatusHandler(JobStore, user);
        }

        public string CreateJob(long userId = UserId) =>
            JobStore.CreateJob(userId, VideoId, "Title", null, null,
                "https://youtube.com/watch?v=dQw4w9WgXcQ");
    }

    [Fact]
    public void ReturnsPending_ForNewlyCreatedJob()
    {
        var fixture = new Fixture();
        var tempId = fixture.CreateJob();

        var result = fixture.Sut.Handle(tempId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("pending");
        result.Value!.Error.Should().BeNull();
    }

    [Fact]
    public void ReturnsRunning_AfterJobClaimed()
    {
        var fixture = new Fixture();
        var tempId = fixture.CreateJob();
        fixture.JobStore.Claim(tempId, UserId);

        var result = fixture.Sut.Handle(tempId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("running");
    }

    [Fact]
    public void ReturnsReady_AfterJobMarkedReady()
    {
        var fixture = new Fixture();
        var tempId = fixture.CreateJob();
        fixture.JobStore.Claim(tempId, UserId);
        fixture.JobStore.MarkReady(tempId, UserId, "/tmp/song.mp3");

        var result = fixture.Sut.Handle(tempId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("ready");
    }

    [Fact]
    public void ReturnsFailed_WithErrorAndCategory_AfterJobMarkedFailed()
    {
        var fixture = new Fixture();
        var tempId = fixture.CreateJob();
        fixture.JobStore.Claim(tempId, UserId);
        fixture.JobStore.MarkFailed(tempId, UserId, "Failed to download video.", "YoutubeDl");

        var result = fixture.Sut.Handle(tempId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("failed");
        result.Value!.Error.Should().Be("Failed to download video.");
        result.Value!.ErrorCategory.Should().Be("YoutubeDl");
    }

    [Fact]
    public void ReturnsNotFound_ForUnknownTempId()
    {
        var fixture = new Fixture();

        var result = fixture.Sut.Handle("does-not-exist");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public void ReturnsNotFound_ForAnotherUsersJob()
    {
        var fixture = new Fixture();
        var tempId = fixture.CreateJob(OtherUserId);

        var result = fixture.Sut.Handle(tempId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }
}
