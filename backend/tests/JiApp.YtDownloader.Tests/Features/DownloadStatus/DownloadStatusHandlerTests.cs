using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Features.DownloadStatus;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.DownloadStatus;

public sealed class DownloadStatusHandlerTests
{
    private const long UserId = 42L;
    private const long OtherUserId = 43L;
    private const string VideoId = "dQw4w9WgXcQ";

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        public DownloadJobStore JobStore { get; }
        public DownloadStatusHandler Sut { get; }

        public Fixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<YtDbContext>()
                .UseSqlite(_connection)
                .Options;
            using (var db = new YtDbContext(options))
                db.Database.Migrate();

            var services = new ServiceCollection();
            services.AddScoped(_ => new YtDbContext(options));
            _provider = services.BuildServiceProvider();

            JobStore = new DownloadJobStore(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                TimeSpan.FromMinutes(15),
                TimeProvider.System);

            var user = Mock.Of<ICurrentUserService>(x => x.UserId == UserId);
            Sut = new DownloadStatusHandler(JobStore, user);
        }

        public string CreateJob(long userId = UserId) =>
            JobStore.CreateJob(userId, VideoId, "Title", null, null,
                "https://youtube.com/watch?v=dQw4w9WgXcQ");

        public void Dispose()
        {
            _provider.Dispose();
            _connection.Dispose();
        }
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
        fixture.JobStore.MarkFailed(tempId, UserId, "Failed to download video.", ResultCategories.YoutubeDl);

        var result = fixture.Sut.Handle(tempId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("failed");
        result.Value!.Error.Should().Be("Failed to download video.");
        result.Value!.ErrorCategory.Should().Be(ResultCategories.YoutubeDl);
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
