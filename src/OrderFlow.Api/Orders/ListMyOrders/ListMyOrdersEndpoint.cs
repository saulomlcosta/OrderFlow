using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Authentication;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Orders.ListMyOrders;

internal static class ListMyOrdersEndpoint
{
    internal static void MapListMyOrders(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me/orders", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal user,
        OrderFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var customerSubject = user.GetSubject();

        if (customerSubject is null)
        {
            return Results.Unauthorized();
        }

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Include("_items")
            .Where(x => x.CustomerSubject == customerSubject)
            .ToListAsync(cancellationToken);

        // SQLite cannot translate DateTimeOffset ordering; history volume is unbounded only after pagination is introduced.
        return Results.Ok(orders
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new OrderResponse(
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
                .ToArray())));
    }
}
