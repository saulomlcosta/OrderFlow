using OrderFlow.Api.Orders.CreateOrder;
using OrderFlow.Api.Orders.GetOrder;

namespace OrderFlow.Api.Orders;

internal static class OrdersModule
{
    internal static IServiceCollection AddOrders(this IServiceCollection services)
    {
        services.AddSingleton<IOrderCreationFailureInjectionHook, NoOpOrderCreationFailureInjectionHook>();

        return services;
    }

    internal static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        app.MapCreateOrder();
        app.MapGetOrder();

        return app;
    }
}
