using OrderFlow.Api.Inventory.AddStock;

namespace OrderFlow.Api.Inventory;

internal static class InventoryModule
{
    internal static IServiceCollection AddInventory(this IServiceCollection services)
    {
        return services;
    }

    internal static IEndpointRouteBuilder MapInventory(this IEndpointRouteBuilder app)
    {
        app.MapAddStock();

        return app;
    }
}
