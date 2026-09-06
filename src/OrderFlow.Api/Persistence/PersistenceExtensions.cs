using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace OrderFlow.Api.Persistence;

internal static class PersistenceExtensions
{
    private const string DevelopmentInMemoryConnectionString = "Data Source=orderflow-dev;Mode=Memory;Cache=Shared";
    private const string PostgreSqlProvider = "PostgreSql";
    private const string SqliteInMemoryProvider = "SqliteInMemory";

    internal static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Persistence:Provider"]
            ?? throw new InvalidOperationException("Persistence:Provider must be configured.");

        if (provider.Equals(SqliteInMemoryProvider, StringComparison.OrdinalIgnoreCase))
        {
            var connection = new SqliteConnection(DevelopmentInMemoryConnectionString);
            connection.Open();

            services.AddSingleton(connection);
            services.AddDbContext<OrderFlowDbContext>(options =>
                options.UseSqlite(connection));

            return services;
        }

        if (!provider.Equals(PostgreSqlProvider, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported persistence provider '{provider}'. Use '{PostgreSqlProvider}' or '{SqliteInMemoryProvider}'.");
        }

        var connectionString = configuration.GetConnectionString("OrderFlow")
            ?? throw new InvalidOperationException("ConnectionStrings:OrderFlow must be configured for PostgreSQL.");

        services.AddDbContext<OrderFlowDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }

    internal static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Persistence:InitializeOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        if (dbContext.Database.IsSqlite())
        {
            await dbContext.Database.EnsureCreatedAsync();
            return;
        }

        await dbContext.Database.MigrateAsync();
    }
}
