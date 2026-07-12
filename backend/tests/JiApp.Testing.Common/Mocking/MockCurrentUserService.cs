namespace JiApp.Testing.Common.Mocking;

public sealed class MockCurrentUserService : MockObject<ICurrentUserService>
{
    public MockCurrentUserService WithReturning(long userId)
    {
        Mock.Setup(x => x.UserId).Returns(userId);
        return this;
    }

    public MockCurrentUserService WithUsername(string username)
    {
        Mock.Setup(x => x.Username).Returns(username);
        return this;
    }

    public static MockCurrentUserService GetSuccessful() =>
        new MockCurrentUserService().WithReturning(1L);
}
