using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Authentication;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Checkouts.ListCheckouts;

internal static class ListCheckoutsEndpoint
{
    internal static void MapListCheckouts(this IEndpointRouteBuilder app)
    {
        app.MapGet("/checkouts", HandleAsync)
            .RequireAuthorization(OrderFlowPolicies.Administrator);
    }

    private static async Task<IResult> HandleAsync(
        string? status,
        OrderFlowDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var normalizedStatus = status?.Trim().ToLowerInvariant();

        var hasSupportedStatus =
            normalizedStatus is null or "" or "active" or "expired" or "completed" or "cancelled";

        if (!hasSupportedStatus)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = ["Unsupported checkout status filter. Use active, expired, completed, or cancelled."]
            });
        }

        var persistedQuery = normalizedStatus switch
        {
            "completed" => dbContext.Checkouts.Where(x => x.Status == CheckoutStatus.Completed),
            "cancelled" => dbContext.Checkouts.Where(x => x.Status == CheckoutStatus.Cancelled),
            "active" or "expired" => dbContext.Checkouts.Where(x =>
                x.Status == CheckoutStatus.Active || x.Status == CheckoutStatus.Expired),
            _ => dbContext.Checkouts
        };

        var checkouts = await persistedQuery.ToListAsync(cancellationToken);

        var filteredCheckouts = normalizedStatus switch
        {
            "active" => checkouts.Where(x => x.Status == CheckoutStatus.Active && !x.IsExpiredAt(now)),
            "expired" => checkouts.Where(x => x.IsExpiredAt(now)),
            _ => checkouts
        };

        return Results.Ok(filteredCheckouts
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ToResponse(now)));
    }
}
