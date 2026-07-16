using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Mocks;

public sealed class MockRoleManager
{
	private readonly Mock<RoleManager<IdentityRole<long>>> _mock;

	private MockRoleManager(Mock<RoleManager<IdentityRole<long>>> mock) => _mock = mock;

	public RoleManager<IdentityRole<long>> Object => _mock.Object;
	public Mock<RoleManager<IdentityRole<long>>> Mock => _mock;

	public static implicit operator RoleManager<IdentityRole<long>>(MockRoleManager m) => m.Object;

	public static MockRoleManager GetSuccessful() => new(new Mock<RoleManager<IdentityRole<long>>>(
		Moq.Mock.Of<IRoleStore<IdentityRole<long>>>(),
		Array.Empty<IRoleValidator<IdentityRole<long>>>(),
		Moq.Mock.Of<ILookupNormalizer>(),
		Moq.Mock.Of<IdentityErrorDescriber>(),
		Moq.Mock.Of<ILogger<RoleManager<IdentityRole<long>>>>()));

	public MockRoleManager WithRoleExistsAsync(string roleName, bool exists)
	{
		_mock.Setup(x => x.RoleExistsAsync(roleName)).ReturnsAsync(exists);
		return this;
	}

	public MockRoleManager WithFindByNameAsync(string roleName, IdentityRole<long>? role)
	{
		_mock.Setup(x => x.FindByNameAsync(roleName)).ReturnsAsync(role);
		return this;
	}

	public MockRoleManager WithFindByNameAsyncForAny(IdentityRole<long>? role)
	{
		_mock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(role);
		return this;
	}

	public MockRoleManager WithCreateAsync(IdentityResult result)
	{
		_mock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole<long>>())).ReturnsAsync(result);
		return this;
	}

	public MockRoleManager WithDeleteAsync(IdentityRole<long> role, IdentityResult result)
	{
		_mock.Setup(x => x.DeleteAsync(role)).ReturnsAsync(result);
		return this;
	}

	public MockRoleManager WithGetClaimsAsync(IdentityRole<long> role, IList<Claim> claims)
	{
		_mock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(claims);
		return this;
	}

	public MockRoleManager WithGetClaimsAsyncByName(string roleName, IList<Claim> claims)
	{
		_mock.Setup(x => x.GetClaimsAsync(It.Is<IdentityRole<long>>(r => r.Name == roleName)))
			.ReturnsAsync(claims);
		return this;
	}

	public MockRoleManager WithGetClaimsAsyncForAny(IList<Claim> claims)
	{
		_mock.Setup(x => x.GetClaimsAsync(It.IsAny<IdentityRole<long>>())).ReturnsAsync(claims);
		return this;
	}

	public MockRoleManager WithAddClaimAsync(IdentityResult result)
	{
		_mock.Setup(x => x.AddClaimAsync(It.IsAny<IdentityRole<long>>(), It.IsAny<Claim>()))
			.ReturnsAsync(result);
		return this;
	}

	public MockRoleManager WithRemoveClaimAsync(IdentityResult result)
	{
		_mock.Setup(x => x.RemoveClaimAsync(It.IsAny<IdentityRole<long>>(), It.IsAny<Claim>()))
			.ReturnsAsync(result);
		return this;
	}

	public MockRoleManager WithRolesQueryable(IQueryable<IdentityRole<long>> queryable)
	{
		_mock.Setup(x => x.Roles).Returns(queryable);
		return this;
	}

	public void VerifyCreatedRole(int count)
	{
		_mock.Verify(x => x.CreateAsync(It.IsAny<IdentityRole<long>>()), Times.Exactly(count));
	}

	public void VerifyRemovedClaimFromRole(string roleName)
	{
		_mock.Verify(
			x => x.RemoveClaimAsync(
				It.Is<IdentityRole<long>>(r => r.Name == roleName),
				It.IsAny<Claim>()),
			Times.AtLeastOnce);
	}

	public void VerifyRemovedClaimFromRole_NotCalled(string roleName)
	{
		_mock.Verify(
			x => x.RemoveClaimAsync(
				It.Is<IdentityRole<long>>(r => r.Name == roleName),
				It.IsAny<Claim>()),
			Times.Never);
	}

	public void VerifyAddedClaimToRole(string roleName)
	{
		_mock.Verify(
			x => x.AddClaimAsync(
				It.Is<IdentityRole<long>>(r => r.Name == roleName),
				It.IsAny<Claim>()),
			Times.AtLeastOnce);
	}

	public void VerifyAddedPermissionToRole(string roleName, string permission)
	{
		_mock.Verify(
			x => x.AddClaimAsync(
				It.Is<IdentityRole<long>>(r => r.Name == roleName),
				It.Is<Claim>(c => c.Type == "permission" && c.Value == permission)),
			Times.Once);
	}

	public void VerifyAddedClaimsToRole(string roleName, int count)
	{
		_mock.Verify(
			x => x.AddClaimAsync(
				It.Is<IdentityRole<long>>(r => r.Name == roleName),
				It.IsAny<Claim>()),
			Times.Exactly(count));
	}

	public void VerifyAddedClaimToRole_NotCalled(string roleName)
	{
		_mock.Verify(
			x => x.AddClaimAsync(
				It.Is<IdentityRole<long>>(r => r.Name == roleName),
				It.IsAny<Claim>()),
			Times.Never);
	}
}
