using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Products.CreateProduct;

internal static class CreateProductEndpoint
{
    internal static void MapCreateProduct(this IEndpointRouteBuilder app)
    {
        app.MapPost("/products", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        CreateProductRequest request,
        OrderFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = Product.Create(request.Name, request.Price);
            var inventory = Inventory.Inventory.ForProduct(product.Id);

            dbContext.Products.Add(product);
            dbContext.Inventories.Add(inventory);
            await dbContext.SaveChangesAsync(cancellationToken);

            var response = await dbContext.Products
                .Where(x => x.Id == product.Id)
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
                .SingleAsync(cancellationToken);

            return Results.Created($"/products/{product.Id}", response);
        }
        catch (DomainValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["product"] = [exception.Message]
            });
        }
    }
}
