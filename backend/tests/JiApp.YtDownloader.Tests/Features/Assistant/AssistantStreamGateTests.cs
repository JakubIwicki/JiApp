using JiApp.YtDownloader.Features.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public sealed class AssistantStreamGateTests
{
    private sealed class Fixture
    {
        public AssistantStreamGate Sut { get; } = new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void TryEnter_WhenGateIsFree_ReturnsTrue()
    {
        var fixture = Fixture.Init();

        var acquired = fixture.Sut.TryEnter();

        acquired.Should().BeTrue();
        fixture.Sut.Release();
    }

    [Fact]
    public void TryEnter_WhenGateIsAlreadyHeld_ReturnsFalse()
    {
        var fixture = Fixture.Init();

        var first = fixture.Sut.TryEnter();
        var second = fixture.Sut.TryEnter();

        first.Should().BeTrue();
        second.Should().BeFalse();

        fixture.Sut.Release();
    }

    [Fact]
    public void Release_AfterAcquire_AllowsNextAcquireToSucceed()
    {
        var fixture = Fixture.Init();

        fixture.Sut.TryEnter();
        fixture.Sut.Release();

        var reacquired = fixture.Sut.TryEnter();
        reacquired.Should().BeTrue();
        fixture.Sut.Release();
    }

    [Fact]
    public void Gate_RegisteredAsSingleton_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AssistantStreamGate>();
        var provider = services.BuildServiceProvider();

        var gate1 = provider.GetRequiredService<AssistantStreamGate>();
        var gate2 = provider.GetRequiredService<AssistantStreamGate>();

        gate1.Should().BeSameAs(gate2);
    }

    [Fact]
    public void ConcurrentStreams_SecondIsBlockedByGate()
    {
        var gate = new AssistantStreamGate();

        var stream1Acquired = gate.TryEnter();
        stream1Acquired.Should().BeTrue();

        var stream2Acquired = gate.TryEnter();
        stream2Acquired.Should().BeFalse();

        gate.Release();

        var stream3Acquired = gate.TryEnter();
        stream3Acquired.Should().BeTrue();
        gate.Release();
    }
}
