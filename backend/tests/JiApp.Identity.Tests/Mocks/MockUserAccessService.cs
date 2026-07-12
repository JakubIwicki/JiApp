using JiApp.Identity.Services;
using Moq;

namespace JiApp.Identity.Tests.Mocks;

public sealed class MockUserAccessService : MockObject<IUserAccessService>
{
	public MockUserAccessService WithGetEffectivePermissionsAsync(long userId, string[] permissions)
	{
		Mock.Setup(x => x.GetEffectivePermissionsAsync(userId)).ReturnsAsync(permissions);
		return this;
	}

	public MockUserAccessService WithFailingDefaultRoleAssignment(long userId, Exception exception)
	{
		Mock.Setup(x => x.AssignDefaultRoleAsync(userId)).ThrowsAsync(exception);
		return this;
	}

	public static MockUserAccessService GetSuccessful() => new();

	public void VerifyAssignedDefaultRole(long userId)
	{
		Mock.Verify(x => x.AssignDefaultRoleAsync(userId), Times.Once);
	}

	public void VerifyAssignedDefaultRole_NotCalled()
	{
		Mock.Verify(x => x.AssignDefaultRoleAsync(It.IsAny<long>()), Times.Never);
	}
}
