using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Health;

[Collection(nameof(OrderFlowApiCollection))]
public sealed class HealthEndpointsTests(OrderFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private readonly OrderFlowApiFactory _factory = factory;

    [Fact]
    public async Task Live_WhenApplicationIsRunning_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_WhenDatabaseIsAvailable_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Probes_WhenDatabaseIsUnavailable_ReportLiveButNotReady()
    {
        await using var unavailableDatabaseFactory = new UnavailableDatabaseApiFactory();
        using var client = unavailableDatabaseFactory.CreateClient();

        var liveResponse = await client.GetAsync("/health/live");
        var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readyResponse.StatusCode);
    }

    private sealed class UnavailableDatabaseApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Persistence:Provider", "PostgreSql");
            builder.UseSetting("Persistence:InitializeOnStartup", "false");
            builder.UseSetting(
                "ConnectionStrings:OrderFlow",
                "Host=localhost;Port=1;Database=orderflow;Username=orderflow;Password=unavailable;Timeout=1");
        }
    }
}
