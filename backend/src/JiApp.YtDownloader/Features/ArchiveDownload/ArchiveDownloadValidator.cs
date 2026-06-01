using FluentValidation;

namespace JiApp.YtDownloader.Features.ArchiveDownload;

public sealed class ArchiveDownloadValidator : AbstractValidator<ArchiveDownloadRequest>
{
    public ArchiveDownloadValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}
