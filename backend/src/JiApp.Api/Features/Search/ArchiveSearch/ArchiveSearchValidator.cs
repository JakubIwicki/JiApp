using FluentValidation;

namespace JiApp.Api.Features.Search.ArchiveSearch;

internal sealed class ArchiveSearchValidator : AbstractValidator<ArchiveSearchRequest>
{
    public ArchiveSearchValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}