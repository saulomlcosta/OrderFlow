using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Authentication;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Checkouts.ListMyCheckouts;

internal static class ListMyCheckoutsEndpoint
{
    internal static void MapListMyCheckouts(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me/checkouts", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal user,
        OrderFlowDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var customerSubject = user.GetSubject();

        if (customerSubject is null)
        {
            return Results.Unauthorized();
        }

        var now = timeProvider.GetUtcNow();
        var checkouts = await dbContext.Checkouts
            .Where(x => x.CustomerSubject == customerSubject)
            .ToListAsync(cancellationToken);

        // SQLite cannot translate DateTimeOffset ordering; history volume is unbounded only after pagination is introduced.
        return Results.Ok(checkouts
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ToResponse(now)));
    }
}
