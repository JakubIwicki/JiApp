using FluentValidation;

namespace JiApp.Identity.Features.Admin.Roles.UpdateRolePermissions;

public sealed class UpdateRolePermissionsValidator : AbstractValidator<UpdateRolePermissionsRequest>
{
	public UpdateRolePermissionsValidator()
	{
		RuleFor(x => x.Permissions).NotNull();
	}
}
