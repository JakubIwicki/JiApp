namespace JiApp.Scheduler.Tests.Domain;

public sealed class BoardTests
{
    private sealed class Fixture
    {
        public static Fixture Init() => new();
    }

    [Fact]
    public void Board_WithValidData_HasCorrectDefaults()
    {
        var board = new Board
        {
            Name = "Test Board",
            MemberUserIds = [1L],
        };

        board.Name.Should().Be("Test Board");
        board.MemberUserIds.Should().ContainSingle().Which.Should().Be(1L);
        board.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
