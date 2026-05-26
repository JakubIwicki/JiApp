using FluentValidation;

namespace JiApp.Api.Features.Downloads.ArchiveDownload;

internal sealed class ArchiveDownloadValidator : AbstractValidator<ArchiveDownloadRequest>
{
    public ArchiveDownloadValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}