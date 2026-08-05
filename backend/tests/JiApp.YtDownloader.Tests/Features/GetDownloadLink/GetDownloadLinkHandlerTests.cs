using System.Threading.Channels;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Features.GetDownloadLink;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.GetDownloadLink;

public sealed class GetDownloadLinkHandlerTests
{
    private const long UserId = 42L;
    private const int TempIdLength = 32;

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        public DownloadJobStore JobStore { get; }
        public Channel<string> Queue { get; } = Channel.CreateUnbounded<string>();
        public GetDownloadLinkHandler Sut { get; }

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

            var user = Mock.Of<ICurrentUserService>(x => x.UserId == UserId && x.Username == "test-user");
            Sut = new GetDownloadLinkHandler(JobStore, Queue, user);
        }

        public void Dispose()
        {
            _provider.Dispose();
            _connection.Dispose();
        }
    }

    private static DownloadRequest CreateRequest() =>
        new("dQw4w9WgXcQ", "https://youtube.com/watch?v=dQw4w9WgXcQ",
            "Title", "Description", "https://example.com/img.jpg");

    [Fact]
    public async Task HandleAsync_ReturnsSuccessWithTempId_WithoutDownloading()
    {
        var fixture = new Fixture();

        var result = await fixture.Sut.HandleAsync(CreateRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.TempId.Should().HaveLength(TempIdLength);
        result.Value!.DownloadUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_EnqueuesJob_OnTheQueue()
    {
        var fixture = new Fixture();

        await fixture.Sut.HandleAsync(CreateRequest());

        fixture.Queue.Reader.Count.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_CreatesPendingJob_WithRequestMetadata()
    {
        var fixture = new Fixture();

        var result = await fixture.Sut.HandleAsync(CreateRequest());
        var job = fixture.JobStore.GetJobInfo(result.Value!.TempId);

        job.Should().NotBeNull();
        job!.UserId.Should().Be(UserId);
        job.VideoId.Should().Be("dQw4w9WgXcQ");
        job.VideoTitle.Should().Be("Title");
        job.VideoDescription.Should().Be("Description");
        job.VideoImageUrl.Should().Be("https://example.com/img.jpg");
        job.VideoUrl.Should().Be("https://youtube.com/watch?v=dQw4w9WgXcQ");
        fixture.JobStore.GetStatus(result.Value.TempId, UserId)!.Status.Should().Be(DownloadJobStatus.Pending);
    }
}
