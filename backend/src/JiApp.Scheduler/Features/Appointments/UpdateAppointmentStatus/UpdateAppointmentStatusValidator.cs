using FluentValidation;

namespace JiApp.Scheduler.Features.Appointments.UpdateAppointmentStatus;

public sealed class UpdateAppointmentStatusValidator : AbstractValidator<UpdateAppointmentStatusRequest>
{
    private static readonly string[] AllowedStatuses = ["done", "cancel", "cancelled"];

    public UpdateAppointmentStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Status must be 'done', 'cancel', or 'cancelled'");
    }
}