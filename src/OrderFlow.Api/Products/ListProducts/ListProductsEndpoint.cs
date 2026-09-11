using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Products.ListProducts;

internal static class ListProductsEndpoint
{
    internal static void MapListProducts(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        OrderFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .OrderBy(x => x.Name)
            .Select(x => new ProductResponse(
                x.Id,
                x.Name,
                x.Price,
                dbContext.Inventories
                    .Where(i => i.ProductId == x.Id)
                    .Select(i => i.Quantity)
                    .Single(),
                dbContext.Inventories
                    .Where(i => i.ProductId == x.Id)
                    .Select(i => i.ReservedQuantity)
                    .Single(),
                dbContext.Inventories
                    .Where(i => i.ProductId == x.Id)
                    .Select(i => i.Quantity - i.ReservedQuantity)
                    .Single()))
            .ToListAsync(cancellationToken);

        return Results.Ok(products);
    }
}
