using OrderFlow.Api.Checkouts.CancelCheckout;
using OrderFlow.Api.Checkouts.CompleteCheckout;
using OrderFlow.Api.Checkouts.ExpireCheckout;
using OrderFlow.Api.Checkouts.GetCheckout;
using OrderFlow.Api.Checkouts.ListCheckouts;
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
        app.MapListCheckouts();
        app.MapGetCheckout();
        app.MapStartCheckout();
        app.MapCancelCheckout();
        app.MapExpireCheckout();
        app.MapCompleteCheckout();

        return app;
    }
}
