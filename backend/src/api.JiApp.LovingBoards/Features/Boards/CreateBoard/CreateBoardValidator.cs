using FluentValidation;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Boards.CreateBoard;

public sealed class CreateBoardValidator : AbstractValidator<CreateBoardRequest>
{
    public CreateBoardValidator(LovingBoardsSettings settings)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(settings.MaxBoardNameLength);
    }
}
