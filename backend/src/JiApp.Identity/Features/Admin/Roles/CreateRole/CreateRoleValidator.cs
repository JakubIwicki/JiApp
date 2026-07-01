using FluentValidation;

namespace JiApp.Identity.Features.Admin.Roles.CreateRole;

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleRequest>
{
	public CreateRoleValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}
