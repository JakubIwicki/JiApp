namespace JiApp.Scheduler.Tests.Domain;

public sealed class AppointmentTests
{
    [Fact]
    public void Appointment_HasDefaultValues()
    {
        var appointment = new Appointment();

        appointment.Id.Should().Be(0L);
        appointment.BoardId.Should().Be(0L);
        appointment.ClientId.Should().Be(0L);
        appointment.ServiceId.Should().Be(0L);
        appointment.Description.Should().BeNull();
        appointment.Location.Should().BeEmpty();
        appointment.Status.Should().Be(AppointmentStatus.Created);
        appointment.CreatedBy.Should().Be(0L);
        appointment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        appointment.Price.Should().NotBeNull();
        appointment.Price.Amount.Should().Be(0m);
        appointment.Price.Currency.Should().Be("PLN");
    }
}