namespace JiApp.Scheduler.Features.Boards.GetBoard;

[Serializable]
public sealed record GetBoardResponse(long Id, string Name, List<long> MemberUserIds, DateTime CreatedAt);
