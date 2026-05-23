using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Mocks;

public static class LoggerMock
{
    public static ILogger<T> Of<T>()
        where T : class
        => Mock.Of<ILogger<T>>();
}