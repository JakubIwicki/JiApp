using FluentValidation;

namespace JiApp.YtDownloader.Features.ArchiveSearch;

public sealed class ArchiveSearchValidator : AbstractValidator<ArchiveSearchRequest>
{
    public ArchiveSearchValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}
