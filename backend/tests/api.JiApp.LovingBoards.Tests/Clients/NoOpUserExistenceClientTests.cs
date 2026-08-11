using api.JiApp.LovingBoards.Clients;

namespace api.JiApp.LovingBoards.Tests.Clients;

public sealed class NoOpUserExistenceClientTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(999_999)]
    public async Task ReportsFound_ForAnyUserId(long userId)
    {
        var client = new NoOpUserExistenceClient();

        var status = await client.CheckExistsAsync(userId, CancellationToken.None);

        status.Should().Be(UserExistenceStatus.Found);
    }
}
