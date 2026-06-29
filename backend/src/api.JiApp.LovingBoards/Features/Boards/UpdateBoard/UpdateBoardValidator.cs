using FluentValidation;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Boards.UpdateBoard;

public sealed class UpdateBoardValidator : AbstractValidator<UpdateBoardRequest>
{
    public UpdateBoardValidator(LovingBoardsSettings settings)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(settings.MaxBoardNameLength);
    }
}
