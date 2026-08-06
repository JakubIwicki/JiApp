using api.JiApp.LovingBoards.Clients;
using JiApp.Testing.Common.Mocking;

namespace api.JiApp.LovingBoards.Tests.Features.Boards.AddBoardMember;

public sealed class UserExistenceClientDouble : MockObject<IUserExistenceClient>
{
    public UserExistenceClientDouble WithStatus(UserExistenceStatus status)
    {
        Mock.Setup(x => x.CheckExistsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);
        return this;
    }

    public static UserExistenceClientDouble Found() => new UserExistenceClientDouble().WithStatus(UserExistenceStatus.Found);

    public static UserExistenceClientDouble NotFound() => new UserExistenceClientDouble().WithStatus(UserExistenceStatus.NotFound);

    public static UserExistenceClientDouble Unavailable() => new UserExistenceClientDouble().WithStatus(UserExistenceStatus.Unavailable);
}
