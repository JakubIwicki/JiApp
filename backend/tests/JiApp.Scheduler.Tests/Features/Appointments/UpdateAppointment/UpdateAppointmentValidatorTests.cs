using JiApp.Scheduler.Features.Appointments.UpdateAppointment;

namespace JiApp.Scheduler.Tests.Features.Appointments.UpdateAppointment;

public sealed class UpdateAppointmentValidatorTests
{
    private readonly UpdateAppointmentValidator _sut = new();

    [Fact]
    public void Validate_WithWeekdayDate_ReturnsError()
    {
        var request = new UpdateAppointmentRequest(
            1, 1,
            new DateOnly(2026, 1, 5),
            new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "", null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithZeroClientId_ReturnsError()
    {
        var request = new UpdateAppointmentRequest(
            0, 1,
            new DateOnly(2026, 1, 3),
            new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "", null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new UpdateAppointmentRequest(
            1, 1,
            new DateOnly(2026, 1, 3),
            new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "", null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}