using FluentValidation;

namespace JiApp.Identity.Features.Admin.Users.AssignRole;

public sealed class AssignRoleValidator : AbstractValidator<AssignRoleRequest>
{
	public AssignRoleValidator()
	{
		RuleFor(x => x.RoleName).NotEmpty();
	}
}
