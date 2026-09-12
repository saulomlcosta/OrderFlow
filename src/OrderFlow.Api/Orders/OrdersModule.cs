using OrderFlow.Api.Orders.GetOrder;
using OrderFlow.Api.Orders.ListMyOrders;

namespace OrderFlow.Api.Orders;

internal static class OrdersModule
{
    internal static IServiceCollection AddOrders(this IServiceCollection services)
    {
        return services;
    }

    internal static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        app.MapGetOrder();
        app.MapListMyOrders();

        return app;
    }
}
