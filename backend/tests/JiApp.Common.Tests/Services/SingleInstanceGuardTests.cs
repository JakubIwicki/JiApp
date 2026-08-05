using System.IO;
using JiApp.Common.Services;

namespace JiApp.Common.Tests.Services;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void Throws_WhenOpeningSamePathTwice()
    {
        var path = TempLockPath("same-path");

        using var first = SingleInstanceGuard.OpenExclusive(path);

        var act = () => SingleInstanceGuard.OpenExclusive(path);

        act.Should().Throw<IOException>();
    }

    [Fact]
    public void Succeeds_WhenReopeningAfterDispose()
    {
        var path = TempLockPath("release");

        var first = SingleInstanceGuard.OpenExclusive(path);
        first.Dispose();

        using var second = SingleInstanceGuard.OpenExclusive(path);

        second.Should().NotBeNull();
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void Succeeds_WhenAcquiringDistinctPaths()
    {
        var firstPath = TempLockPath("distinct-a");
        var secondPath = TempLockPath("distinct-b");

        using var first = SingleInstanceGuard.AcquireAt(firstPath);
        using var second = SingleInstanceGuard.AcquireAt(secondPath);

        first.Should().NotBeSameAs(second);
        File.Exists(firstPath).Should().BeTrue();
        File.Exists(secondPath).Should().BeTrue();
    }

    [Fact]
    public void ReturnsSameStream_WhenAcquiringSamePathTwice()
    {
        var path = TempLockPath("idempotent");

        var first = SingleInstanceGuard.AcquireAt(path);
        var second = SingleInstanceGuard.AcquireAt(path);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void ReturnsDistinctPaths_ForDistinctServiceNames()
    {
        var gateway = SingleInstanceGuard.GetLockPath("gateway");
        var identity = SingleInstanceGuard.GetLockPath("identity");

        gateway.Should().NotBe(identity);
        gateway.Should().StartWith(Path.Combine(AppContext.BaseDirectory, "data"));
        gateway.Should().EndWith("gateway.instance.lock");
    }

    private static string TempLockPath(string suffix) =>
        Path.Combine(Path.GetTempPath(), $"singleguard-{suffix}-{Guid.NewGuid():N}.lock");
}
