namespace JiApp.Scheduler.Tests.Domain;

public sealed class ClientTests
{
    [Fact]
    public void Client_HasDefaultValues()
    {
        var client = new Client();

        client.Id.Should().Be(0L);
        client.BoardId.Should().Be(0L);
        client.Name.Should().BeEmpty();
        client.Phone.Should().BeNull();
        client.Notes.Should().BeNull();
        client.Appointments.Should().BeEmpty();
    }
}