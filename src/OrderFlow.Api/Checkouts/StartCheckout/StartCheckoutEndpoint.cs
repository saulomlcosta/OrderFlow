using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Checkouts;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Checkouts.StartCheckout;

internal static class StartCheckoutEndpoint
{
    internal static void MapStartCheckout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/checkouts", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        StartCheckoutRequest request,
        OrderFlowDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRequest(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var requestedItems = request.Items
            .GroupBy(x => x.ProductId)
            .Select(group => new StartCheckoutItemRequest(group.Key, group.Sum(x => x.Quantity)))
            .ToList();

        var productIds = requestedItems.Select(x => x.ProductId).ToList();

        var products = await dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["One or more referenced products do not exist."]
            });
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            foreach (var item in requestedItems)
            {
                var affectedRows = await dbContext.Inventories
                    .Where(x => x.ProductId == item.ProductId && x.Quantity - x.ReservedQuantity >= item.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            inventory => inventory.ReservedQuantity,
                            inventory => inventory.ReservedQuantity + item.Quantity),
                        cancellationToken);

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["stock"] = [$"Not enough available stock for product {item.ProductId}."]
                    });
                }
            }

            var checkout = Checkout.Start(
                requestedItems.Select(item => CheckoutItem.Create(item.ProductId, item.Quantity)),
                createdAt: timeProvider.GetUtcNow());

            dbContext.Checkouts.Add(checkout);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Created(
                $"/checkouts/{checkout.Id}",
                checkout.ToResponse(timeProvider.GetUtcNow()));
        }
        catch (DomainValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["checkout"] = [exception.Message]
            });
        }
    }

    private static Dictionary<string, string[]> ValidateRequest(StartCheckoutRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Items is null || request.Items.Count == 0)
        {
            errors["items"] = ["At least one checkout item is required."];
            return errors;
        }

        var invalidItems = request.Items
            .Where(x => x.ProductId == Guid.Empty || x.Quantity <= 0)
            .ToList();

        if (invalidItems.Count > 0)
        {
            errors["items"] = ["Each checkout item must include a valid product id and quantity greater than zero."];
        }

        return errors;
    }
}
