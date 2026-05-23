using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Services;
using JiApp.YtApi;
using Moq;

namespace JiApp.Tests.Mocks;

public static class CurrentUserServiceMock
{
    public static Mock<ICurrentUserService> GetSuccessful(long userId = 1L)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId);
        return mock;
    }

    public static Mock<ICurrentUserService> GetWithUsername(long userId, string username)
    {
        var mock = GetSuccessful(userId);
        mock.Setup(x => x.Username).Returns(username);
        return mock;
    }
}

public static class TempFileStoreMock
{
    public static Mock<ITempFileStore> GetSuccessful()
        => new();
}

public static class YoutubeClientMock
{
    public static Mock<IYoutubeClient> GetSuccessful()
        => new();
}