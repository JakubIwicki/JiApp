using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Services.CreateService;

public sealed class CreateServiceHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(CreateServiceRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, request.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        if (!Enum.TryParse<ServiceCategory>(request.Category, ignoreCase: true, out var category))
            return Result<long>.Failure($"Invalid service category: {request.Category}", ResultCategories.Validation);

        var service = new Service
        {
            BoardId = request.BoardId,
            Name = request.Name,
            Category = category,
            BaseDuration = request.BaseDuration,
            BasePrice = new Price(request.BasePrice.Amount, request.BasePrice.Currency)
        };
        db.Services.Add(service);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(service.Id);
    }
}