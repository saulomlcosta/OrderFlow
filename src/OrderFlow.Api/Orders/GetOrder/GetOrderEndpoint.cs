using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Authentication;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Orders.GetOrder;

internal static class GetOrderEndpoint
{
    internal static void MapGetOrder(this IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{id:guid}", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ClaimsPrincipal user,
        OrderFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var customerSubject = user.GetSubject();

        if (customerSubject is null)
        {
            return Results.Unauthorized();
        }

        var order = await dbContext.Orders
            .AsNoTracking()
            .Include("_items")
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null ||
            (!user.IsAdministrator() && order.CustomerSubject != customerSubject))
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
