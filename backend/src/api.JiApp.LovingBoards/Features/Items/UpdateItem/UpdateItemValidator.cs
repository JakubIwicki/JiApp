using FluentValidation;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Items.UpdateItem;

public sealed class UpdateItemValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemValidator(LovingBoardsSettings settings)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(settings.MaxItemTitleLength);
        RuleFor(x => x.Quantity).MaximumLength(settings.MaxQuantityLength);
        RuleFor(x => x.Category).MaximumLength(settings.MaxCategoryLength);
        RuleFor(x => x.Note).MaximumLength(settings.MaxNoteLength);
    }
}
