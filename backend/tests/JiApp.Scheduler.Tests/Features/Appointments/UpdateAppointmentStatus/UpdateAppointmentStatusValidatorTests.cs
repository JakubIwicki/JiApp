using JiApp.Scheduler.Features.Appointments.UpdateAppointmentStatus;

namespace JiApp.Scheduler.Tests.Features.Appointments.UpdateAppointmentStatus;

public sealed class UpdateAppointmentStatusValidatorTests
{
    private sealed class Fixture
    {
        public UpdateAppointmentStatusValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithEmptyStatus_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateAppointmentStatusRequest("");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithDone_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new UpdateAppointmentStatusRequest("done");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCancel_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new UpdateAppointmentStatusRequest("cancel");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCancelled_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new UpdateAppointmentStatusRequest("cancelled");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidStatus_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateAppointmentStatusRequest("invalid");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
