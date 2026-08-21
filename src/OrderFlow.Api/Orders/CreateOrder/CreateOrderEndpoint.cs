using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Orders.CreateOrder;

internal static class CreateOrderEndpoint
{
    internal static void MapCreateOrder(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        CreateOrderRequest request,
        OrderFlowDbContext dbContext,
        IOrderCreationFailureInjectionHook failureInjectionHook,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRequest(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var requestedItems = request.Items
            .GroupBy(x => x.ProductId)
            .Select(group => new CreateOrderItemRequest(group.Key, group.Sum(x => x.Quantity)))
            .ToList();

        var productIds = requestedItems.Select(x => x.ProductId).ToList();

        var products = await dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["One or more referenced products do not exist."]
            });
        }

        var inventories = await dbContext.Inventories
            .Where(x => productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);

        foreach (var item in requestedItems)
        {
            if (!inventories.TryGetValue(item.ProductId, out var inventory))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["items"] = [$"Inventory was not found for product {item.ProductId}."]
                });
            }

            if (inventory.Quantity < item.Quantity)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["stock"] = [$"Not enough stock for product {item.ProductId}."]
                });
            }
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            foreach (var item in requestedItems)
            {
                var affectedRows = await dbContext.Inventories
                    .Where(x => x.ProductId == item.ProductId && x.Quantity >= item.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            inventory => inventory.Quantity,
                            inventory => inventory.Quantity - item.Quantity),
                        cancellationToken);

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["stock"] = [$"Not enough stock for product {item.ProductId}."]
                    });
                }
            }

            await failureInjectionHook.AfterStockDecrementAsync(cancellationToken);

            var orderItems = requestedItems
                .Select(item =>
                {
                    var product = products[item.ProductId];
                    return OrderItem.Create(product.Id, product.Name, product.Price, item.Quantity);
                })
                .ToList();

            var order = Order.Create(orderItems);

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Created($"/orders/{order.Id}", new { order.Id });
        }
        catch (DomainValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["order"] = [exception.Message]
            });
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "Order creation failed before completion.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static Dictionary<string, string[]> ValidateRequest(CreateOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Items is null || request.Items.Count == 0)
        {
            errors["items"] = ["At least one order item is required."];
            return errors;
        }

        var invalidItems = request.Items
            .Where(x => x.ProductId == Guid.Empty || x.Quantity <= 0)
            .ToList();

        if (invalidItems.Count > 0)
        {
            errors["items"] = ["Each order item must include a valid product id and quantity greater than zero."];
        }

        return errors;
    }
}
