using JiApp.Scheduler.Domain;

namespace JiApp.Scheduler.Features.Common;

internal static class AppointmentDomainHelpers
{
    internal static bool IsWeekendDate(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    internal static Price ResolvePrice(PriceRequest? priceRequest, Price basePrice) =>
        priceRequest is not null
            ? new Price(priceRequest.Amount, priceRequest.Currency)
            : new Price(basePrice.Amount, basePrice.Currency);
}