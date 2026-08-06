using JiApp.Common.Authentication;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Boards.CreateBoard;

namespace api.JiApp.LovingBoards.Tests.Features.Boards.CreateBoard;

public sealed class CreateBoardHandlerTests : HandlerTestBase<LovingBoardsDbContext>
{
    private static readonly LovingBoardsSettings DefaultSettings = new()
    {
        ConnectionString = "Data Source=:memory:",
        Jwt = new JwtSettings { Key = "key", Issuer = "iss", Audience = "aud" },
        MaxBoardsPerUser = 2,
        DefaultPageSize = 50,
        MaxBoardNameLength = 200
    };

    private sealed class Fixture
    {
        private readonly ILovingBoardsDbContext _dbContext;
        private readonly TestDb _testDb;
        private readonly ICurrentUserService _currentUser;
        private readonly LovingBoardsSettings _settings;
        private readonly TimeProvider _timeProvider = TimeProvider.System;

        private Fixture(ILovingBoardsDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
            _settings = DefaultSettings;
        }

        public CreateBoardHandler Sut => new(_dbContext, _settings, _currentUser, _timeProvider, new UserWriteLock());

        public static Fixture Init(ILovingBoardsDbContext dbContext, TestDb testDb) => new(dbContext, testDb);
    }

    [Fact]
    public async Task CreateBoard_UnderCap_ReturnsBoardId_WithCreatorAsOwnerAndMember()
    {
        var fixture = Fixture.Init(DbContext, Db);

        var result = await fixture.Sut.HandleAsync(new CreateBoardRequest("My Board"), CancellationToken.None);

        var boardId = AssertSuccess(result);
        var board = Db.Find<Board>(boardId);
        board.Should().NotBeNull();
        board!.Name.Should().Be("My Board");
        board.OwnerUserId.Should().Be(1L);
        board.MemberUserIds.Should().ContainSingle().Which.Should().Be(1L);
    }

    [Fact]
    public async Task CreateBoard_CapReached_ReturnsValidationFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);

        for (var i = 0; i < DefaultSettings.MaxBoardsPerUser; i++)
            await fixture.Sut.HandleAsync(new CreateBoardRequest($"Board {i}"), CancellationToken.None);

        var result = await fixture.Sut.HandleAsync(new CreateBoardRequest("Over Limit"), CancellationToken.None);

        AssertValidationFailure(result);
        result.Error.Should().Contain("Maximum number of boards");
        AssertEntityCount<Board>((LovingBoardsDbContext)DbContext, DefaultSettings.MaxBoardsPerUser);
    }
}
