using FluentValidation;

namespace JiApp.Scheduler.Features.Boards.CreateBoard;

public sealed class CreateBoardValidator : AbstractValidator<CreateBoardRequest>
{
    public CreateBoardValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
