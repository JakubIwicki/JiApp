using System.Security.Claims;
using JiApp.Common;
using JiApp.Common.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace JiApp.Common.Tests.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Succeeds_WhenUserHoldsPermissionClaim()
    {
        var context = await Act(
            UserWithClaims(
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(Permissions.PermissionClaimType, Permissions.SchedulerAccess)),
            new PermissionRequirement(Permissions.SchedulerAccess));

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Succeeds_WhenUserIsAdmin_WithoutPermissionClaim()
    {
        var context = await Act(
            UserWithClaims(
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Role, RoleNames.Admin)),
            new PermissionRequirement(Permissions.SchedulerAccess));

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_WhenUserHoldsOnlyOtherPermissionClaim()
    {
        var context = await Act(
            UserWithClaims(
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(Permissions.PermissionClaimType, Permissions.YtDownloaderAccess)),
            new PermissionRequirement(Permissions.SchedulerAccess));

        context.HasSucceeded.Should().BeFalse();
        context.PendingRequirements.Should().ContainSingle().Which.Should()
            .BeOfType<PermissionRequirement>().Which.Permission.Should().Be(Permissions.SchedulerAccess);
    }

    [Fact]
    public async Task Fails_WhenAuthenticatedUser_WithoutPermissionClaim()
    {
        var context = await Act(
            UserWithClaims(new Claim(ClaimTypes.NameIdentifier, "42")),
            new PermissionRequirement(Permissions.SchedulerAccess));

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenAnonymousUser()
    {
        var context = await Act(new ClaimsPrincipal(), new PermissionRequirement(Permissions.SchedulerAccess));

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Succeeds_WhenAllRequiredPermissionsHeld()
    {
        var context = await Act(
            UserWithClaims(
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(Permissions.PermissionClaimType, Permissions.SchedulerAccess),
                new Claim(Permissions.PermissionClaimType, Permissions.YtDownloaderAccess)),
            new PermissionRequirement(Permissions.SchedulerAccess),
            new PermissionRequirement(Permissions.YtDownloaderAccess));

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_WhenOnlyOneOfTwoRequiredPermissionsHeld()
    {
        var context = await Act(
            UserWithClaims(
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(Permissions.PermissionClaimType, Permissions.SchedulerAccess)),
            new PermissionRequirement(Permissions.SchedulerAccess),
            new PermissionRequirement(Permissions.YtDownloaderAccess));

        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeFalse();
        context.PendingRequirements.Should().ContainSingle().Which.Should()
            .BeOfType<PermissionRequirement>().Which.Permission.Should().Be(Permissions.YtDownloaderAccess);
    }

    [Fact]
    public async Task Succeeds_AsAdmin_WhenMultiplePermissionsRequired()
    {
        var context = await Act(
            UserWithClaims(
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Role, RoleNames.Admin)),
            new PermissionRequirement(Permissions.SchedulerAccess),
            new PermissionRequirement(Permissions.YtDownloaderAccess));

        context.HasSucceeded.Should().BeTrue();
    }

    private static async Task<AuthorizationHandlerContext> Act(
        ClaimsPrincipal user, params PermissionRequirement[] requirements)
    {
        var context = new AuthorizationHandlerContext(requirements, user, resource: null);
        await new PermissionAuthorizationHandler().HandleAsync(context);
        return context;
    }

    private static ClaimsPrincipal UserWithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
