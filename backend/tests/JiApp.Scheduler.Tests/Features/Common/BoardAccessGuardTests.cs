using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Common;

public sealed class BoardAccessGuardTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SchedulerDbContext _db;
    private readonly Mock<ICurrentUserService> _currentUser;

    public BoardAccessGuardTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SchedulerDbContext(options);
        _db.Database.EnsureCreated();

        _currentUser = new Mock<ICurrentUserService>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
    }

    [Fact]
    public async Task VerifyBoardAccess_WhenBoardExistsAndUserIsMember_ReturnsBoard()
    {
        _currentUser.Setup(x => x.UserId).Returns(1L);
        var board = new Board { Name = "Test", MemberUserIds = [1L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var result =
            await BoardAccessGuard.VerifyBoardAccessAsync(_db, board.Id, _currentUser.Object, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(board.Id);
        result.Value.Name.Should().Be("Test");
    }

    [Fact]
    public async Task VerifyBoardAccess_WhenBoardDoesNotExist_ReturnsNotFound()
    {
        _currentUser.Setup(x => x.UserId).Returns(1L);

        var result =
            await BoardAccessGuard.VerifyBoardAccessAsync(_db, 999L, _currentUser.Object, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task VerifyBoardAccess_WhenUserIsNotMember_ReturnsAccessDenied()
    {
        _currentUser.Setup(x => x.UserId).Returns(1L);
        var board = new Board { Name = "Test", MemberUserIds = [2L] };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        var result =
            await BoardAccessGuard.VerifyBoardAccessAsync(_db, board.Id, _currentUser.Object, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}