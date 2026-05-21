using FluentAssertions;
using JiApp.Infrastructure.Services;

namespace JiApp.Tests.Infrastructure;

public class TempFileStoreTests
{
    [Fact]
    public void Add_StoresFileAndReturnsId()
    {
        var store = new TempFileStore(TimeSpan.FromMinutes(10));
        var tempFile = Path.GetTempFileName();
        try
        {
            var id = store.Add(tempFile);

            id.Should().NotBeNullOrEmpty();
            Guid.TryParse(id, out _).Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_ReturnsPath_ForValidId()
    {
        var store = new TempFileStore(TimeSpan.FromMinutes(10));
        var tempFile = Path.GetTempFileName();
        try
        {
            var id = store.Add(tempFile);

            var path = store.Get(id);

            path.Should().Be(tempFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_ReturnsNull_ForInvalidId()
    {
        var store = new TempFileStore(TimeSpan.FromMinutes(10));

        var path = store.Get("nonexistent-id");

        path.Should().BeNull();
    }

    [Fact]
    public void Get_ReturnsNull_ForExpiredEntry()
    {
        var store = new TempFileStore(TimeSpan.FromDays(-1));
        var tempFile = Path.GetTempFileName();
        try
        {
            var id = store.Add(tempFile);

            var path = store.Get(id);

            path.Should().BeNull();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CleanupExpired_RemovesExpiredEntriesAndDeletesFiles()
    {
        var store = new TempFileStore(TimeSpan.FromDays(-1));
        var tempFile = Path.GetTempFileName();
        try
        {
            var id = store.Add(tempFile);

            store.CleanupExpired();

            File.Exists(tempFile).Should().BeFalse();
            store.Get(id).Should().BeNull();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
