using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Features.DownloadFile;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.DownloadFile;

public sealed class DownloadFileHandlerTests : HandlerTestBase<YtDbContext>
{
    private const long UserId = 1L;
    private const long OtherUserId = 2L;
    private const string VideoId = "dQw4w9WgXcQ";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    [Fact]
    public void ReturnsFile_ForReadyJobOwnedByCaller()
    {
        var fixture = Fixture.Init(DbContext).WithReadyJob(out var tempId);

        var result = fixture.Sut.Handle(tempId);

        AssertSuccess(result).Should().Be(fixture.FilePath);
    }

    [Fact]
    public void ReturnsNotFound_ForUnknownTempId()
    {
        var fixture = Fixture.Init(DbContext);

        var result = fixture.Sut.Handle("does-not-exist");

        AssertNotFound(result);
        fixture.JobStore.GetStatus("does-not-exist", UserId).Should().BeNull();
    }

    [Fact]
    public void ReturnsNotFound_WhenJobOwnedByAnotherUser()
    {
        var fixture = Fixture.Init(DbContext).WithReadyJob(out var tempId, ownerUserId: OtherUserId);

        var result = fixture.Sut.Handle(tempId);

        AssertNotFound(result);
        fixture.JobStore.GetStatus(tempId, OtherUserId)!.Status.Should().Be(DownloadJobStatus.Ready);
    }

    [Fact]
    public void ReturnsNotFound_WhenFileGone()
    {
        var fixture = Fixture.Init(DbContext).WithReadyJob(out var tempId);
        File.Delete(fixture.FilePath);

        var result = fixture.Sut.Handle(tempId);

        AssertNotFound(result);
        fixture.JobStore.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Ready);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly MockCurrentUserService _currentUser;

        public DownloadJobStore JobStore { get; }
        public string TempDir { get; }
        public string FilePath { get; private set; } = default!;

        public Fixture(YtDbContext dbContext, long userId)
        {
            _currentUser = new MockCurrentUserService().WithReturning(userId);
            var services = new ServiceCollection();
            // Singleton (not scoped): the store opens a scope per operation and the container
            // disposes scoped services at scope end, which would dispose the shared base context.
            services.AddSingleton(dbContext);
            _provider = services.BuildServiceProvider();
            TempDir = Directory.CreateTempSubdirectory("ytdl-downloadfile-tests-").FullName;
            JobStore = new DownloadJobStore(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                Ttl,
                TimeProvider.System,
                TimeSpan.FromMinutes(35),
                TempDir);
        }

        public DownloadFileHandler Sut => new(JobStore, _currentUser.Object, Mock.Of<ILogger<DownloadFileHandler>>());

        public static Fixture Init(YtDbContext dbContext, long userId = UserId) => new(dbContext, userId);

        public Fixture WithReadyJob(out string tempId, long ownerUserId = UserId)
        {
            tempId = JobStore.CreateJob(
                ownerUserId, VideoId, "Title", null, null, "https://youtube.com/watch?v=dQw4w9WgXcQ");
            JobStore.Claim(tempId, ownerUserId);
            FilePath = CreateFile();
            JobStore.MarkReady(tempId, ownerUserId, FilePath);
            return this;
        }

        public string CreateFile(string fileName = "song.mp3")
        {
            var path = Path.Combine(TempDir, fileName);
            File.WriteAllBytes(path, [0x49, 0x44, 0x33]);
            return path;
        }

        public void Dispose()
        {
            _provider.Dispose();
            try
            {
                Directory.Delete(TempDir, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
