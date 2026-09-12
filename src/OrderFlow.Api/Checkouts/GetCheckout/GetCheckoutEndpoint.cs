using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Authentication;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Checkouts.GetCheckout;

internal static class GetCheckoutEndpoint
{
    internal static void MapGetCheckout(this IEndpointRouteBuilder app)
    {
        app.MapGet("/checkouts/{id:guid}", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
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

        var checkout = await dbContext.Checkouts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (checkout is null ||
            (!user.IsAdministrator() && checkout.CustomerSubject != customerSubject))
        {
            return Results.NotFound();
        }

        return Results.Ok(checkout.ToResponse(timeProvider.GetUtcNow()));
    }
}
