using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Orders.GetOrder;

internal static class GetOrderEndpoint
{
    internal static void MapGetOrder(this IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{id:guid}", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        OrderFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include("_items")
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
        {
            return Results.NotFound();
        }

        var response = new OrderResponse(
            order.Id,
            order.CheckoutId,
            order.CreatedAt,
            order.Total,
            order.Items
                .Select(item => new OrderItemResponse(
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity))
                .ToArray());

        return Results.Ok(response);
    }
}
