using JiApp.Scheduler.Domain;

namespace JiApp.Scheduler.Features.Common;

internal static class Validators
{
    internal static bool BeValidServiceCategory(string category) =>
        Enum.TryParse<ServiceCategory>(category, ignoreCase: true, out _);
}
