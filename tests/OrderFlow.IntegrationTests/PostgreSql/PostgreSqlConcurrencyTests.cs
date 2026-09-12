using System.Net;
using System.Net.Http.Json;
using OrderFlow.Api.Checkouts;
using OrderFlow.Api.Products;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.PostgreSql;

[Collection(nameof(PostgreSqlApiCollection))]
public sealed class PostgreSqlConcurrencyTests(PostgreSqlApiFactory factory)
    : PostgreSqlIntegrationTestBase(factory)
{
    [PostgreSqlFact]
    public async Task StartCheckout_WithTwentyConcurrentBuyers_ReservesOnlyAvailableStock()
    {
        using var administratorClient = Factory.CreateAdministratorClient();
        using var client = Factory.CreateCustomerClient();
        var productId = await CreateProductWithStockAsync(administratorClient, stockQuantity: 5);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var requests = Enumerable.Range(0, 20)
            .Select(async _ =>
            {
                await start.Task;
                return await StartCheckoutAsync(client, productId);
            })
            .ToArray();

        start.SetResult();
        var responses = await Task.WhenAll(requests);
        var product = await client.GetFromJsonAsync<ProductResponse>($"/products/{productId}");

        Assert.Equal(5, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(15, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
        Assert.NotNull(product);
        Assert.Equal(5, product.StockQuantity);
        Assert.Equal(5, product.ReservedStockQuantity);
        Assert.Equal(0, product.AvailableStockQuantity);
    }

    [PostgreSqlFact]
    public async Task CompleteCheckout_WithTenConcurrentRequests_CreatesOneOrder()
    {
        using var administratorClient = Factory.CreateAdministratorClient();
        using var client = Factory.CreateCustomerClient();
        var productId = await CreateProductWithStockAsync(administratorClient, stockQuantity: 1);
        var checkoutResponse = await StartCheckoutAsync(client, productId);
        checkoutResponse.EnsureSuccessStatusCode();
        var checkout = await checkoutResponse.Content.ReadFromJsonAsync<CheckoutResponse>();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var requests = Enumerable.Range(0, 10)
            .Select(async _ =>
            {
                await start.Task;
                return await client.PostAsync($"/checkouts/{checkout!.Id}/complete", content: null);
            })
            .ToArray();

        start.SetResult();
        var responses = await Task.WhenAll(requests);
        var product = await client.GetFromJsonAsync<ProductResponse>($"/products/{productId}");
        var createdResponse = Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Created);
        var createdOrder = await createdResponse.Content.ReadFromJsonAsync<CreatedOrderResponse>();

        Assert.Equal(9, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
        Assert.NotNull(createdOrder);
        Assert.Equal(checkout!.Id, createdOrder.CheckoutId);
        Assert.NotNull(product);
        Assert.Equal(0, product.StockQuantity);
        Assert.Equal(0, product.ReservedStockQuantity);
        Assert.Equal(0, product.AvailableStockQuantity);
        Assert.Equal(1, await Factory.CountOrdersAsync());
    }

    private static async Task<Guid> CreateProductWithStockAsync(HttpClient client, int stockQuantity)
    {
        var createResponse = await client.PostAsJsonAsync("/products", new
        {
            Name = "PostgreSQL Limited Product",
            Price = 100m
        });

        createResponse.EnsureSuccessStatusCode();
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var stockResponse = await client.PostAsJsonAsync($"/products/{product!.Id}/stock", new
        {
            Quantity = stockQuantity
        });

        stockResponse.EnsureSuccessStatusCode();
        return product.Id;
    }

    private static Task<HttpResponseMessage> StartCheckoutAsync(HttpClient client, Guid productId)
    {
        return client.PostAsJsonAsync("/checkouts", new
        {
            Items = new[]
            {
                new
                {
                    ProductId = productId,
                    Quantity = 1
                }
            }
        });
    }

    private sealed record CreatedOrderResponse(Guid Id, Guid CheckoutId);
}
