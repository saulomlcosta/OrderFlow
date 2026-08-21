using OrderFlow.Api.Checkouts.CancelCheckout;
using OrderFlow.Api.Checkouts.CompleteCheckout;
using OrderFlow.Api.Checkouts.StartCheckout;

namespace OrderFlow.Api.Checkouts;

internal static class CheckoutsModule
{
    internal static IServiceCollection AddCheckouts(this IServiceCollection services)
    {
        return services;
    }

    internal static IEndpointRouteBuilder MapCheckouts(this IEndpointRouteBuilder app)
    {
        app.MapStartCheckout();
        app.MapCancelCheckout();
        app.MapCompleteCheckout();

        return app;
    }
}
