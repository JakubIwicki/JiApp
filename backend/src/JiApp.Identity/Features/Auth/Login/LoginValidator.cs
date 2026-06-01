using FluentValidation;
using JiApp.Common.Constants;

namespace JiApp.Identity.Features.Auth.Login;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(ValidationConstants.LoginUsernameMaxLength);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(ValidationConstants.PasswordMaxLength);
    }
}
