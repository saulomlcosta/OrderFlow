using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Products.GetProduct;

internal static class GetProductEndpoint
{
    internal static void MapGetProduct(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products/{id:guid}", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        OrderFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Where(x => x.Id == id)
            .Select(x => new ProductResponse(
                x.Id,
                x.Name,
                x.Price,
                dbContext.Inventories
                    .Where(i => i.ProductId == x.Id)
                    .Select(i => i.Quantity)
                    .Single()))
            .SingleOrDefaultAsync(cancellationToken);

        return product is null ? Results.NotFound() : Results.Ok(product);
    }
}
