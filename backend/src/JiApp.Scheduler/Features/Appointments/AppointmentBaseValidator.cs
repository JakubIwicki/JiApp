using FluentValidation;
using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Appointments;

public abstract class AppointmentBaseValidator<T> : AbstractValidator<T> where T : IAppointmentRequest
{
    protected AppointmentBaseValidator()
    {
        RuleFor(x => x.ClientId).GreaterThan(0);
        RuleFor(x => x.ServiceId).GreaterThan(0);
        RuleFor(x => x.Date)
            .Must(d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            .WithMessage("Date must be a weekend day (Saturday or Sunday)");
        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("StartTime must be before EndTime");
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}
