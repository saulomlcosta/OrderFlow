using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Persistence;

namespace OrderFlow.IntegrationTests.Infrastructure;

[Collection(nameof(OrderFlowApiCollection))]
public sealed class DatabaseIsolationTests : IntegrationTestBase
{
    private readonly OrderFlowApiFactory _factory;
    private readonly HttpClient _client;

    public DatabaseIsolationTests(OrderFlowApiFactory factory)
        : base(factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("First isolated product")]
    [InlineData("Second isolated product")]
    public async Task Database_StartsEmpty_ForEveryTest(string productName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        Assert.Equal(0, await dbContext.Products.CountAsync());

        var response = await _client.PostAsJsonAsync("/products", new
        {
            Name = productName,
            Price = 100m
        });

        response.EnsureSuccessStatusCode();
    }
}
