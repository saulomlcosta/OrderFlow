using OrderFlow.Api.Products.CreateProduct;
using OrderFlow.Api.Products.GetProduct;
using OrderFlow.Api.Products.ListProducts;

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
        app.MapListProducts();

        return app;
    }
}
