namespace JiApp.Testing.Common.Mocking;

public abstract class MockObject<T> where T : class
{
    public readonly Mock<T> Mock = new();

    public static implicit operator T(MockObject<T> mock) => mock.Mock.Object;
}
