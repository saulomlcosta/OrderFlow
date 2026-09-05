using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Checkouts.GetCheckout;

internal static class GetCheckoutEndpoint
{
    internal static void MapGetCheckout(this IEndpointRouteBuilder app)
    {
        app.MapGet("/checkouts/{id:guid}", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        OrderFlowDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var checkout = await dbContext.Checkouts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (checkout is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(checkout.ToResponse(timeProvider.GetUtcNow()));
    }
}
