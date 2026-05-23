using System.Threading.Tasks;
using JiApp.Infrastructure.Repositories;
using Moq;

namespace JiApp.Tests.Mocks;

public static class DownloadHistoryRepositoryMock
{
    public static Mock<IDownloadHistoryRepository> GetSuccessful()
    {
        var mock = new Mock<IDownloadHistoryRepository>();
        mock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        return mock;
    }
}

public static class SearchHistoryRepositoryMock
{
    public static Mock<ISearchHistoryRepository> GetSuccessful()
    {
        var mock = new Mock<ISearchHistoryRepository>();
        mock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        return mock;
    }
}