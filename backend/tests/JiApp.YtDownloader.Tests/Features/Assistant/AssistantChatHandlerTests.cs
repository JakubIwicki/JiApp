using JiApp.YtDownloader.Features.Assistant;
using JiApp.YtDownloader.Repositories;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public class AssistantChatHandlerTests
{
    private const long UserId = 99L;
    private const int DailyLimit = 30;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    private sealed class Fixture
    {
        private bool _quotaAvailable = true;
        private bool _clientConfigured = true;

        public Mock<IAssistantUsageRepository> Usage { get; } = new();
        public Mock<IAssistantChatClientProvider> Provider { get; } = new();

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

        public AssistantChatHandler Build()
        {
            Usage.Setup(u => u.TryConsumeAsync(UserId, DailyLimit, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_quotaAvailable);
            Provider.SetupGet(p => p.IsConfigured).Returns(_clientConfigured);
            return new AssistantChatHandler(Usage.Object, Provider.Object);
        }
    }

    [Fact]
    public async Task PreCheckAsync_returns_ok_when_quota_available_and_client_configured()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture();
        var handler = fixture.Build();

        var outcome = await handler.PreCheckAsync(UserId, DailyLimit, cts.Token);

        outcome.Should().Be(AssistantChatPreCheck.Ok);
    }

    [Fact]
    public async Task PreCheckAsync_returns_quota_exceeded_and_does_not_touch_client()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture().WithQuotaExceeded();
        var handler = fixture.Build();

        var outcome = await handler.PreCheckAsync(UserId, DailyLimit, cts.Token);

        outcome.Should().Be(AssistantChatPreCheck.QuotaExceeded);
        fixture.Provider.VerifyGet(p => p.Client, Times.Never);
        fixture.Provider.VerifyGet(p => p.IsConfigured, Times.Never);
    }

    [Fact]
    public async Task PreCheckAsync_returns_not_configured_when_quota_ok_but_client_absent()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture().WithClientNotConfigured();
        var handler = fixture.Build();

        var outcome = await handler.PreCheckAsync(UserId, DailyLimit, cts.Token);

        outcome.Should().Be(AssistantChatPreCheck.NotConfigured);
    }

    [Fact]
    public async Task PreCheckAsync_consumes_quota_before_checking_configuration()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture();
        var handler = fixture.Build();

        await handler.PreCheckAsync(UserId, DailyLimit, cts.Token);

        fixture.Usage.Verify(
            u => u.TryConsumeAsync(UserId, DailyLimit, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
