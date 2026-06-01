using FluentValidation;

namespace JiApp.Scheduler.Features.Appointments.CreateAppointment;

public sealed class CreateAppointmentValidator : AppointmentBaseValidator<CreateAppointmentRequest>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.BoardId).GreaterThan(0);
    }
}