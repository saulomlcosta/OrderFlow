using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Checkouts.CancelCheckout;

internal static class CancelCheckoutEndpoint
{
    internal static void MapCancelCheckout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/checkouts/{id:guid}/cancel", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        OrderFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var checkout = await dbContext.Checkouts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (checkout is null)
        {
            return Results.NotFound();
        }

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
                        ["checkout"] = ["The reservation could not be released consistently."]
                    });
                }
            }

            checkout.Cancel();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Ok(new CheckoutResponse(
                checkout.Id,
                checkout.CreatedAt,
                checkout.ExpiresAt,
                checkout.Status.ToString(),
                checkout.Items
                    .Select(item => new CheckoutItemResponse(item.ProductId, item.Quantity))
                    .ToList()));
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
