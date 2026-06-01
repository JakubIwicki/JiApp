using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Clients.GetClient;

public sealed class GetClientHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<GetClientResponse>> HandleAsync(long id, CancellationToken ct)
    {
        var client = await db.Clients
            .Include(c => c.Board)
            .Include(c => c.Appointments)
            .ThenInclude(a => a.Service)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (client is null)
            return Result<GetClientResponse>.Failure("Client not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, client.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<GetClientResponse>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var response = new GetClientResponse(
            client.Id,
            client.Name,
            client.Phone,
            client.Notes,
            client.Appointments.Select(a => new AppointmentSummary(
                a.Id,
                a.Date,
                a.StartTime,
                a.EndTime,
                a.Service.Name,
                a.Status.ToString()
            )).ToList()
        );

        return Result<GetClientResponse>.Success(response);
    }
}