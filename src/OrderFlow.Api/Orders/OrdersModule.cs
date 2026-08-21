using OrderFlow.Api.Orders.GetOrder;

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

        return app;
    }
}
