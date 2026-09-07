using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Common;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Checkouts.CompleteCheckout;

internal static class CompleteCheckoutEndpoint
{
    internal static void MapCompleteCheckout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/checkouts/{id:guid}/complete", HandleAsync);
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

        var productIds = checkout.Items.Select(x => x.ProductId).ToList();
        var products = await dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["checkout"] = ["One or more products referenced by this checkout no longer exist."]
            });
        }

        var now = timeProvider.GetUtcNow();

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            foreach (var item in checkout.Items)
            {
                var affectedRows = await dbContext.Inventories
                    .Where(x =>
                        x.ProductId == item.ProductId &&
                        x.ReservedQuantity >= item.Quantity &&
                        x.Quantity >= item.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(inventory => inventory.Quantity, inventory => inventory.Quantity - item.Quantity)
                            .SetProperty(inventory => inventory.ReservedQuantity, inventory => inventory.ReservedQuantity - item.Quantity),
                        cancellationToken);

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["checkout"] = ["The reserved stock could not be confirmed consistently."]
                    });
                }
            }

            var order = Order.Create(
                checkout.Id,
                checkout.Items.Select(item =>
                {
                    var product = products[item.ProductId];
                    return OrderItem.Create(product.Id, product.Name, product.Price, item.Quantity);
                }).ToList());

            checkout.Complete(now);

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Created($"/orders/{order.Id}", new { order.Id, CheckoutId = checkout.Id });
        }
        catch (DomainValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["checkout"] = [exception.Message]
            });
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "Checkout completion failed before completion.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
