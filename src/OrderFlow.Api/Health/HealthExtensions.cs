using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace OrderFlow.Api.Health;

internal static class HealthExtensions
{
    private const string ReadinessTag = "ready";

    internal static IServiceCollection AddOrderFlowHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [ReadinessTag]);

        return services;
    }

    internal static IEndpointRouteBuilder MapOrderFlowHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadinessTag)
        });

        return app;
    }
}
