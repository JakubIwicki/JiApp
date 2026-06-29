using FluentValidation;

namespace api.JiApp.LovingBoards.Features.Boards.AddBoardMember;

public sealed class AddBoardMemberValidator : AbstractValidator<AddBoardMemberRequest>
{
    public AddBoardMemberValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}
