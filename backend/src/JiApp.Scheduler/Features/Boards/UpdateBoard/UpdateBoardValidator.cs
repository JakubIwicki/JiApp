using FluentValidation;

namespace JiApp.Scheduler.Features.Boards.UpdateBoard;

public sealed class UpdateBoardValidator : AbstractValidator<UpdateBoardRequest>
{
    public UpdateBoardValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
