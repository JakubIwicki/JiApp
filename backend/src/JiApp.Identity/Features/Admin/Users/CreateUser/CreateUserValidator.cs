using FluentValidation;
using JiApp.Common.Constants;

namespace JiApp.Identity.Features.Admin.Users.CreateUser;

public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
	public CreateUserValidator()
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
			.MinimumLength(ValidationConstants.PasswordMinLength)
			.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
			.Matches("[0-9]").WithMessage("Password must contain at least one digit.");

		RuleFor(x => x.DisplayName)
			.NotEmpty()
			.MaximumLength(ValidationConstants.DisplayNameMaxLength);
	}
}
