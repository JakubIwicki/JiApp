using JiApp.Scheduler.Features.Appointments.CreateAppointment;

namespace JiApp.Scheduler.Tests.Features.Appointments.CreateAppointment;

public sealed class CreateAppointmentValidatorTests
{
    private sealed class Fixture
    {
        public CreateAppointmentValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithWeekdayDate_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateAppointmentRequest(
            1, 1, 1,
            new DateOnly(2026, 1, 5),
            new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "", null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithStartTimeAfterEndTime_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateAppointmentRequest(
            1, 1, 1,
            new DateOnly(2026, 1, 3),
            new TimeOnly(11, 0), new TimeOnly(10, 0),
            null, "", null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithZeroClientId_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateAppointmentRequest(
            1, 0, 1,
            new DateOnly(2026, 1, 3),
            new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "", null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new CreateAppointmentRequest(
            1, 1, 1,
            new DateOnly(2026, 1, 3),
            new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "", null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
