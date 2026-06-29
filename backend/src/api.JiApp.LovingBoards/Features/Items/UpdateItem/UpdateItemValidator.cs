using FluentValidation;
using api.JiApp.LovingBoards.Common;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Items.UpdateItem;

public sealed class UpdateItemValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemValidator(LovingBoardsSettings settings)
    {
        RuleFor(x => x.Title.Value)
            .NotEmpty().WithMessage("Title is required when provided")
            .MaximumLength(settings.MaxItemTitleLength)
            .When(x => x.Title.IsSet);

        RuleFor(x => x.Quantity.Value)
            .MaximumLength(settings.MaxQuantityLength)
            .When(x => x.Quantity.IsSet);

        RuleFor(x => x.Category.Value)
            .MaximumLength(settings.MaxCategoryLength)
            .When(x => x.Category.IsSet);

        RuleFor(x => x.Note.Value)
            .MaximumLength(settings.MaxNoteLength)
            .When(x => x.Note.IsSet);
    }
}
