namespace JiApp.Scheduler.Tests.Domain;

public sealed class ClientTests
{
    private sealed class Fixture
    {
        public static Fixture Init() => new();
    }

    [Fact]
    public void Client_HasDefaultValues()
    {
        Fixture.Init();
        var client = new Client();

        client.Id.Should().Be(0L);
        client.BoardId.Should().Be(0L);
        client.Name.Should().BeEmpty();
        client.Phone.Should().BeNull();
        client.Notes.Should().BeNull();
        client.Appointments.Should().BeEmpty();
    }
}
