using OrderFlow.Api.Products.CreateProduct;
using OrderFlow.Api.Products.GetProduct;

namespace OrderFlow.Api.Products;

internal static class ProductsModule
{
    internal static IServiceCollection AddProducts(this IServiceCollection services)
    {
        return services;
    }

    internal static IEndpointRouteBuilder MapProducts(this IEndpointRouteBuilder app)
    {
        app.MapCreateProduct();
        app.MapGetProduct();

        return app;
    }
}
