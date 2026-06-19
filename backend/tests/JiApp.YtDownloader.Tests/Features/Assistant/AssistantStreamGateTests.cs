using JiApp.YtDownloader.Features.Assistant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public class AssistantStreamGateTests
{
    private static AssistantStreamGate CreateSut() => new();

    [Fact]
    public void TryEnter_succeeds_when_gate_is_free()
    {
        var sut = CreateSut();
        var acquired = sut.TryEnter();
        acquired.Should().BeTrue();
        sut.Release();
    }

    [Fact]
    public void TryEnter_fails_when_gate_is_already_held()
    {
        var sut = CreateSut();
        var first = sut.TryEnter();
        var second = sut.TryEnter();

        first.Should().BeTrue();
        second.Should().BeFalse();

        sut.Release();
    }

    [Fact]
    public void Release_allows_next_acquire_to_succeed()
    {
        var sut = CreateSut();

        sut.TryEnter();
        sut.Release();

        var reacquired = sut.TryEnter();
        reacquired.Should().BeTrue();
        sut.Release();
    }

    [Fact]
    public void Gate_registered_as_singleton_returns_same_instance()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AssistantStreamGate>();
        var provider = services.BuildServiceProvider();

        var gate1 = provider.GetRequiredService<AssistantStreamGate>();
        var gate2 = provider.GetRequiredService<AssistantStreamGate>();

        gate1.Should().BeSameAs(gate2);
    }

    [Fact]
    public void Concurrent_streams_second_is_blocked_by_gate()
    {
        // Simulate the endpoint's gate pattern:
        // Stream 1 acquires → Stream 2 fails with 503 → Stream 1 releases → Stream 3 succeeds.

        var gate = new AssistantStreamGate();

        // Stream 1 enters
        var stream1Acquired = gate.TryEnter();
        stream1Acquired.Should().BeTrue();

        // Stream 2 tries simultaneously — must fail
        var stream2Acquired = gate.TryEnter();
        stream2Acquired.Should().BeFalse();

        // Stream 1 finishes
        gate.Release();

        // Stream 3 now succeeds
        var stream3Acquired = gate.TryEnter();
        stream3Acquired.Should().BeTrue();
        gate.Release();
    }
}
