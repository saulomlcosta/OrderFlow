using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderFlow.Api.Orders.CreateOrder;
using OrderFlow.Api.Persistence;

namespace OrderFlow.IntegrationTests.Infrastructure;

public sealed class OrderFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    public ConfigurableOrderCreationFailureInjectionHook FailureInjectionHook { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrderFlowDbContext>>();
            services.RemoveAll<OrderFlowDbContext>();
            services.RemoveAll<IOrderCreationFailureInjectionHook>();
            services.AddDbContext<OrderFlowDbContext>(options =>
                options.UseSqlite(_connection.ConnectionString));
            services.AddSingleton<IOrderCreationFailureInjectionHook>(FailureInjectionHook);

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=orderflow-tests;Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
