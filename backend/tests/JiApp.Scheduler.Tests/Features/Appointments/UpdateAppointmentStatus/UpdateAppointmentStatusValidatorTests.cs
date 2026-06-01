using JiApp.Scheduler.Features.Appointments.UpdateAppointmentStatus;

namespace JiApp.Scheduler.Tests.Features.Appointments.UpdateAppointmentStatus;

public sealed class UpdateAppointmentStatusValidatorTests
{
    private readonly UpdateAppointmentStatusValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyStatus_ReturnsError()
    {
        var request = new UpdateAppointmentStatusRequest("");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithDone_IsValid()
    {
        var request = new UpdateAppointmentStatusRequest("done");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCancel_IsValid()
    {
        var request = new UpdateAppointmentStatusRequest("cancel");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCancelled_IsValid()
    {
        var request = new UpdateAppointmentStatusRequest("cancelled");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidStatus_ReturnsError()
    {
        var request = new UpdateAppointmentStatusRequest("invalid");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}