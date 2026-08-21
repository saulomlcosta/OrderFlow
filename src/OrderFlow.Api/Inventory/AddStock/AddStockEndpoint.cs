using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;
using OrderFlow.Api.Products;

namespace OrderFlow.Api.Inventory.AddStock;

internal static class AddStockEndpoint
{
    internal static void MapAddStock(this IEndpointRouteBuilder app)
    {
        app.MapPost("/products/{id:guid}/stock", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        AddStockRequest request,
        OrderFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (product is null)
        {
            return Results.NotFound();
        }

        var inventory = await dbContext.Inventories.SingleAsync(x => x.ProductId == id, cancellationToken);

        try
        {
            inventory.Add(request.Quantity);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new ProductResponse(
                product.Id,
                product.Name,
                product.Price,
                inventory.Quantity,
                inventory.ReservedQuantity,
                inventory.AvailableQuantity));
        }
        catch (DomainValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["stock"] = [exception.Message]
            });
        }
    }
}
