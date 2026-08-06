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
        appointment.CreatedAt.Should().Be(default(DateTime));
        appointment.Price.Should().NotBeNull();
        appointment.Price.Amount.Should().Be(0m);
        appointment.Price.Currency.Should().Be("PLN");
    }

    [Fact]
    public void Succeeds_Transitioning_CreatedToDone()
    {
        var appointment = CreatedAppointment();

        var result = appointment.TryTransitionTo(AppointmentStatus.Done);

        result.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Done);
    }

    [Fact]
    public void Succeeds_Transitioning_CreatedToCancelled()
    {
        var appointment = CreatedAppointment();

        var result = appointment.TryTransitionTo(AppointmentStatus.Cancelled);

        result.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void Fails_Transitioning_CreatedToCreated()
    {
        var appointment = CreatedAppointment();

        var result = appointment.TryTransitionTo(AppointmentStatus.Created);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Created");
        appointment.Status.Should().Be(AppointmentStatus.Created);
    }

    [Fact]
    public void Fails_Transitioning_AfterAlreadyTransitioned()
    {
        var appointment = CreatedAppointment();
        appointment.TryTransitionTo(AppointmentStatus.Done);

        var result = appointment.TryTransitionTo(AppointmentStatus.Cancelled);

        result.IsSuccess.Should().BeFalse();
        appointment.Status.Should().Be(AppointmentStatus.Done);
    }

    private static Appointment CreatedAppointment() => new()
    {
        BoardId = 1,
        ClientId = 1,
        ServiceId = 1,
        Date = new DateOnly(2026, 1, 31),
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(11, 0),
        Price = new Price(100),
        CreatedBy = 1
    };
}
