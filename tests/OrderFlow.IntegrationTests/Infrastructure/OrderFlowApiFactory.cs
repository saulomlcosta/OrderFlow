using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderFlow.Api.Persistence;

namespace OrderFlow.IntegrationTests.Infrastructure;

public sealed class OrderFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly DateTimeOffset InitialUtcNow = new(2026, 8, 26, 3, 0, 0, TimeSpan.Zero);
    private SqliteConnection _connection = null!;
    private readonly MutableTimeProvider _timeProvider = new(InitialUtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Persistence:Provider", "SqliteInMemory");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(_timeProvider);
            services.RemoveAll<SqliteConnection>();
            services.RemoveAll<DbContextOptions<OrderFlowDbContext>>();
            services.RemoveAll<OrderFlowDbContext>();
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme,
                    _ => { });
            services.AddSingleton(_connection);
            services.AddDbContext<OrderFlowDbContext>(options =>
                options.UseSqlite(_connection.ConnectionString));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection(
            "Data Source=orderflow-tests;Mode=Memory;Cache=Shared;Pooling=False");
        await _connection.OpenAsync();
        ResetTime();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    internal void ResetTime()
    {
        _timeProvider.SetUtcNow(InitialUtcNow);
    }

    internal void AdvanceTime(TimeSpan duration)
    {
        _timeProvider.Advance(duration);
    }

    internal async Task ResetStateAsync()
    {
        ResetTime();

        await _connection.CloseAsync();
        await _connection.OpenAsync();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
