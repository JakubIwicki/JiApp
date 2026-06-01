using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Services.UpdateService;

public sealed class UpdateServiceHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, UpdateServiceRequest request, CancellationToken ct)
    {
        var service = await db.Services.FindAsync([id], ct);
        if (service is null)
            return Result<long>.Failure("Service not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, service.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        if (!Enum.TryParse<ServiceCategory>(request.Category, ignoreCase: true, out var category))
            return Result<long>.Failure($"Invalid service category: {request.Category}", ResultCategories.Validation);

        service.Name = request.Name;
        service.Category = category;
        service.BaseDuration = request.BaseDuration;
        service.BasePrice = new Price(request.BasePrice.Amount, request.BasePrice.Currency);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(service.Id);
    }
}