using JiApp.Common.Constants;
using JiApp.Common.Models;
using JiApp.Identity.Persistence;
using JiApp.Identity.Services;
using JiApp.Identity.Tests.Mocks;
using JiApp.Testing.Common.Data;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Tests.Services;

/// <summary>
/// Real-context suite for UserAccessService.GetEffectivePermissionsAsync: the
/// single-join permission resolution (G10.4) is verified against a real
/// IdentityDbContext — role claims, cross-role deduplication, and the missing-
/// user case all run through the production query.
/// </summary>
public sealed class UserAccessServiceTests : HandlerTestBase<IdentityDbContext>
{
    private const string EditorRole = "Editor";
    private const long EditorUserId = 1;

    [Fact]
    public async Task GetEffectivePermissionsAsync_ReturnsRolePermissionClaims()
    {
        var service = Fixture.Init(DbContext).WithEditorPermissions().Build();

        var permissions = await service.GetEffectivePermissionsAsync(EditorUserId, CancellationToken.None);

        permissions.Should().BeEquivalentTo([Permissions.SchedulerAccess, Permissions.UsersManage]);
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_DeduplicatesPermissionsAcrossRoles()
    {
        var service = Fixture.Init(DbContext)
            .WithEditorPermissions()
            .WithSecondRoleGranting(Permissions.SchedulerAccess)
            .Build();

        var permissions = await service.GetEffectivePermissionsAsync(EditorUserId, CancellationToken.None);

        permissions.Should().BeEquivalentTo([Permissions.SchedulerAccess, Permissions.UsersManage]);
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_IgnoresNonPermissionClaims()
    {
        var service = Fixture.Init(DbContext)
            .WithEditorPermissions()
            .WithEditorRoleClaim("session_count", "7")
            .Build();

        var permissions = await service.GetEffectivePermissionsAsync(EditorUserId, CancellationToken.None);

        permissions.Should().BeEquivalentTo([Permissions.SchedulerAccess, Permissions.UsersManage]);
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_ReturnsEmpty_WhenUserDoesNotExist()
    {
        var service = Fixture.Init(DbContext).WithEditorPermissions().Build();

        var permissions = await service.GetEffectivePermissionsAsync(999, CancellationToken.None);

        permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_ResolvesMultipleRolesInOneQuery()
    {
        using var counting = SqliteQueryCountFixture<IdentityDbContext>.Create();
        var service = Fixture.Init(counting.Db)
            .WithEditorPermissions()
            .WithSecondRoleGranting(Permissions.SchedulerAccess)
            .Build();
        counting.Interceptor.Reset();

        var permissions = await service.GetEffectivePermissionsAsync(EditorUserId, CancellationToken.None);

        permissions.Should().BeEquivalentTo([Permissions.SchedulerAccess, Permissions.UsersManage]);
        counting.Interceptor.Count.Should().Be(1);
    }

    private sealed class Fixture
    {
        private readonly IdentityDbContext _db;
        private long _editorRoleId;

        private Fixture(IdentityDbContext db) => _db = db;

        public static Fixture Init(IdentityDbContext db) => new(db);

        public Fixture WithEditorPermissions()
        {
            var role = new IdentityRole<long>(EditorRole);
            _db.Roles.Add(role);
            _db.SaveChanges();
            _editorRoleId = role.Id;

            _db.RoleClaims.Add(new IdentityRoleClaim<long>
            {
                RoleId = role.Id,
                ClaimType = Permissions.PermissionClaimType,
                ClaimValue = Permissions.SchedulerAccess
            });
            _db.RoleClaims.Add(new IdentityRoleClaim<long>
            {
                RoleId = role.Id,
                ClaimType = Permissions.PermissionClaimType,
                ClaimValue = Permissions.UsersManage
            });

            _db.Users.Add(new User { Id = EditorUserId, UserName = "alice" });
            _db.UserRoles.Add(new IdentityUserRole<long> { UserId = EditorUserId, RoleId = role.Id });

            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithSecondRoleGranting(string permission)
        {
            var role = new IdentityRole<long>("Viewer");
            _db.Roles.Add(role);
            _db.SaveChanges();

            _db.RoleClaims.Add(new IdentityRoleClaim<long>
            {
                RoleId = role.Id,
                ClaimType = Permissions.PermissionClaimType,
                ClaimValue = permission
            });
            _db.UserRoles.Add(new IdentityUserRole<long> { UserId = EditorUserId, RoleId = role.Id });

            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithEditorRoleClaim(string claimType, string claimValue)
        {
            _db.RoleClaims.Add(new IdentityRoleClaim<long>
            {
                RoleId = _editorRoleId,
                ClaimType = claimType,
                ClaimValue = claimValue
            });
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public UserAccessService Build() => new(MockUserManager.GetSuccessful().Object, _db);
    }
}
