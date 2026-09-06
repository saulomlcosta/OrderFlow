using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Persistence;

namespace OrderFlow.IntegrationTests.Infrastructure;

public sealed class PostgreSqlApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=orderflow;Username=orderflow;Password=orderflow-dev";

    internal static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("ORDERFLOW_RUN_POSTGRESQL_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ORDERFLOW_POSTGRESQL_CONNECTION_STRING")
            ?? DefaultConnectionString;

        builder.UseEnvironment("Development");
        builder.UseSetting("Persistence:Provider", "PostgreSql");
        builder.UseSetting("Persistence:InitializeOnStartup", "true");
        builder.UseSetting("ConnectionStrings:OrderFlow", connectionString);
    }

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        using var client = CreateClient();
        await ResetStateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    internal async Task ResetStateAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "CheckoutItems",
                "OrderItems",
                "Checkouts",
                "Orders",
                "Inventories",
                "Products"
            RESTART IDENTITY CASCADE;
            """);
    }

    internal async Task<int> CountOrdersAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        return await dbContext.Orders.CountAsync();
    }
}
