using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OrderFlow.Api.Persistence;

namespace OrderFlow.IntegrationTests.Infrastructure;

public sealed class PostgreSqlApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=orderflow_tests;Username=orderflow;Password=orderflow-dev";

    internal static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("ORDERFLOW_RUN_POSTGRESQL_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Persistence:Provider", "PostgreSql");
        builder.UseSetting("Persistence:InitializeOnStartup", "true");
        builder.UseSetting("ConnectionStrings:OrderFlow", GetConnectionString());
        builder.ConfigureServices(services => services.AddTestAuthentication());
    }

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        await EnsureTestDatabaseExistsAsync();
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

    internal HttpClient CreateAdministratorClient() => CreateClient().AsAdministrator();

    private static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("ORDERFLOW_POSTGRESQL_CONNECTION_STRING")
        ?? DefaultConnectionString;

    private static async Task EnsureTestDatabaseExistsAsync()
    {
        var testConnection = new NpgsqlConnectionStringBuilder(GetConnectionString());
        var testDatabase = testConnection.Database;

        if (string.IsNullOrWhiteSpace(testDatabase))
        {
            throw new InvalidOperationException("The PostgreSQL test connection must specify a database.");
        }

        var adminConnection = new NpgsqlConnectionStringBuilder(testConnection.ConnectionString)
        {
            Database = "postgres"
        };

        await using var connection = new NpgsqlConnection(adminConnection.ConnectionString);
        await connection.OpenAsync();

        await using var existsCommand = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName);",
            connection);
        existsCommand.Parameters.AddWithValue("databaseName", testDatabase);

        if (await existsCommand.ExecuteScalarAsync() is true)
        {
            return;
        }

        using var commandBuilder = new NpgsqlCommandBuilder();
        var quotedDatabase = commandBuilder.QuoteIdentifier(testDatabase);
        await using var createCommand = new NpgsqlCommand($"CREATE DATABASE {quotedDatabase};", connection);
        await createCommand.ExecuteNonQueryAsync();
    }
}
