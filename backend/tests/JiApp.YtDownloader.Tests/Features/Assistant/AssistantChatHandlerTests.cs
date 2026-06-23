using JiApp.YtDownloader.Features.Assistant;
using JiApp.YtDownloader.Repositories;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public sealed class AssistantChatHandlerTests
{
    private const long UserId = 99L;
    private const int DailyLimit = 30;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    private sealed class Fixture
    {
        private bool _quotaAvailable = true;
        private bool _clientConfigured = true;

        private AssistantChatHandler? _sut;

        public Mock<IAssistantUsageRepository> UsageMock { get; } = new();
        public Mock<IAssistantChatClientProvider> ProviderMock { get; } = new();

        public AssistantChatHandler Sut
        {
            get
            {
                if (_sut is null)
                {
                    UsageMock.Setup(u => u.TryConsumeAsync(UserId, DailyLimit, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_quotaAvailable);
                    ProviderMock.SetupGet(p => p.IsConfigured).Returns(_clientConfigured);
                    _sut = new AssistantChatHandler(UsageMock.Object, ProviderMock.Object);
                }
                return _sut;
            }
        }

        public Fixture WithQuotaExceeded()
        {
            _quotaAvailable = false;
            return this;
        }

        public Fixture WithClientNotConfigured()
        {
            _clientConfigured = false;
            return this;
        }
    }

    [Fact]
    public async Task PreCheckAsync_WithQuotaAndClientConfigured_ReturnsOk()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture();

        var outcome = await fixture.Sut.PreCheckAsync(UserId, DailyLimit, cts.Token);

        outcome.Should().Be(AssistantChatPreCheck.Ok);
    }

    [Fact]
    public async Task PreCheckAsync_WithQuotaExceeded_ReturnsQuotaExceeded()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture().WithQuotaExceeded();

        var outcome = await fixture.Sut.PreCheckAsync(UserId, DailyLimit, cts.Token);

        outcome.Should().Be(AssistantChatPreCheck.QuotaExceeded);
        fixture.ProviderMock.VerifyGet(p => p.Client, Times.Never);
    }

    [Fact]
    public async Task PreCheckAsync_WithClientNotConfigured_ReturnsNotConfiguredWithoutConsumingQuota()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture().WithClientNotConfigured();

        var outcome = await fixture.Sut.PreCheckAsync(UserId, DailyLimit, cts.Token);

        outcome.Should().Be(AssistantChatPreCheck.NotConfigured);
        fixture.UsageMock.Verify(
            u => u.TryConsumeAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PreCheckAsync_ChecksConfiguration_BeforeConsumingQuota()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture();

        await fixture.Sut.PreCheckAsync(UserId, DailyLimit, cts.Token);

        fixture.ProviderMock.VerifyGet(p => p.IsConfigured, Times.Once);
        fixture.UsageMock.Verify(
            u => u.TryConsumeAsync(UserId, DailyLimit, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
