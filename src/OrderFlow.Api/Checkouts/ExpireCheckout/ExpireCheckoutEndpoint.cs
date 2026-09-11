using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Authentication;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Checkouts.ExpireCheckout;

internal static class ExpireCheckoutEndpoint
{
    internal static void MapExpireCheckout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/checkouts/{id:guid}/expire", HandleAsync)
            .RequireAuthorization(OrderFlowPolicies.Administrator);
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

        var now = timeProvider.GetUtcNow();

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            foreach (var item in checkout.Items)
            {
                var affectedRows = await dbContext.Inventories
                    .Where(x => x.ProductId == item.ProductId && x.ReservedQuantity >= item.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            inventory => inventory.ReservedQuantity,
                            inventory => inventory.ReservedQuantity - item.Quantity),
                        cancellationToken);

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["checkout"] = ["The expired reservation could not be released consistently."]
                    });
                }
            }

            checkout.Expire(now);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Ok(checkout.ToResponse(now));
        }
        catch (DomainValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["checkout"] = [exception.Message]
            });
        }
    }
}
