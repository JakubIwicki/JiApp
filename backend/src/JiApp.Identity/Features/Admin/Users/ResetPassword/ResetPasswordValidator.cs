using FluentValidation;
using JiApp.Common.Constants;

namespace JiApp.Identity.Features.Admin.Users.ResetPassword;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
	public ResetPasswordValidator()
	{
		RuleFor(x => x.NewPassword)
			.NotEmpty()
			.MinimumLength(ValidationConstants.PasswordMinLength)
			.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
			.Matches("[0-9]").WithMessage("Password must contain at least one digit.");
	}
}
