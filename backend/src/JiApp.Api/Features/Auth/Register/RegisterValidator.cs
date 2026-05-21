using FluentValidation;
using JiApp.Common.Constants;

namespace JiApp.Api.Features.Auth.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(ValidationConstants.UsernameMinLength, ValidationConstants.UsernameMaxLength)
            .Matches("^[a-zA-Z0-9_]+$");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(ValidationConstants.PasswordMinLength);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(ValidationConstants.DisplayNameMaxLength);
    }
}
