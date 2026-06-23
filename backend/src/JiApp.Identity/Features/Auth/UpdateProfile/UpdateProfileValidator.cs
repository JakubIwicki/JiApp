using FluentValidation;
using JiApp.Common.Constants;

namespace JiApp.Identity.Features.Auth.UpdateProfile;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(ValidationConstants.DisplayNameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
