using FluentValidation;
using api.JiApp.LovingBoards.Domain;

namespace api.JiApp.LovingBoards.Features.Items.SetItemStatus;

public sealed class SetItemStatusValidator : AbstractValidator<SetItemStatusRequest>
{
    public SetItemStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => Enum.TryParse<BoardItemStatus>(s, ignoreCase: true, out _))
            .WithMessage("Status must be 'Needed', 'Completed', or 'Removed'");
    }
}
