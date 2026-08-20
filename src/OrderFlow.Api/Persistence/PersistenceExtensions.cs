using Microsoft.EntityFrameworkCore;

namespace OrderFlow.Api.Persistence;

internal static class PersistenceExtensions
{
    private const string DefaultConnectionString = "Data Source=orderflow.db";

    internal static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("OrderFlow")
            ?? DefaultConnectionString;

        services.AddDbContext<OrderFlowDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }

    internal static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
