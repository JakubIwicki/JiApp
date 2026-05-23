using FluentValidation;

namespace JiApp.Api.Features.History.GetHistory;

public sealed class GetHistoryValidator : AbstractValidator<GetHistoryRequest>
{
    public GetHistoryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .When(x => x.Limit.HasValue);
    }
}