using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Persistence;
using OrderFlow.Api.Products;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Orders;

[Collection(nameof(OrderFlowApiCollection))]
public class OrderEndpointsTests(OrderFlowApiFactory factory)
{
    private readonly OrderFlowApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateOrder_WithEnoughStock_Succeeds_AndDecreasesInventory()
    {
        var productId = await CreateProductAsync("Camera", 500m);
        await AddStockAsync(productId, 5);

        var createOrderResponse = await _client.PostAsJsonAsync("/orders", new
        {
            Items = new[]
            {
                new
                {
                    ProductId = productId,
                    Quantity = 2
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, createOrderResponse.StatusCode);

        var getProductResponse = await _client.GetAsync($"/products/{productId}");
        var product = await getProductResponse.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);
        Assert.Equal(3, product.StockQuantity);
        Assert.Equal(0, product.ReservedStockQuantity);
        Assert.Equal(3, product.AvailableStockQuantity);
    }

    [Fact]
    public async Task CreateOrder_WithoutEnoughStock_Fails()
    {
        var productId = await CreateProductAsync("Printer", 900m);
        await AddStockAsync(productId, 1);

        var response = await _client.PostAsJsonAsync("/orders", new
        {
            Items = new[]
            {
                new
                {
                    ProductId = productId,
                    Quantity = 2
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCreatedOrder_Succeeds_AndPreservesProductSnapshot()
    {
        var productId = await CreateProductAsync("Monitor", 500m);
        await AddStockAsync(productId, 4);

        var createOrderResponse = await _client.PostAsJsonAsync("/orders", new
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

        createOrderResponse.EnsureSuccessStatusCode();

        var created = await createOrderResponse.Content.ReadFromJsonAsync<CreatedOrderResponse>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var product = await dbContext.Products.FindAsync(productId);
        product!.UpdateCatalogDetails("Monitor Pro", 600m);
        await dbContext.SaveChangesAsync();

        var getOrderResponse = await _client.GetAsync($"/orders/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getOrderResponse.StatusCode);

        var order = await getOrderResponse.Content.ReadFromJsonAsync<OrderResponse>();
        var item = Assert.Single(order!.Items);

        Assert.Equal(500m, item.UnitPrice);
        Assert.Equal("Monitor", item.ProductName);
        Assert.Equal(500m, order.Total);
    }

    [Fact]
    public async Task CreateOrder_WithConcurrentBuyers_AllowsOnlyOneOrderAgainstOneUnit()
    {
        var productId = await CreateProductAsync("Limited Console", 3000m);
        await AddStockAsync(productId, 1);

        var ordersBefore = await CountOrdersAsync();

        var firstClient = _factory.CreateClient();
        var secondClient = _factory.CreateClient();

        var firstOrderTask = firstClient.PostAsJsonAsync("/orders", new
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

        var secondOrderTask = secondClient.PostAsJsonAsync("/orders", new
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

        var responses = await Task.WhenAll(firstOrderTask, secondOrderTask);

        var successfulOrders = responses.Count(response => response.StatusCode == HttpStatusCode.Created);
        var failedOrders = responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest);
        var ordersAfter = await CountOrdersAsync();
        var product = await GetProductAsync(productId);

        Assert.Equal(1, successfulOrders);
        Assert.Equal(1, failedOrders);
        Assert.Equal(ordersBefore + 1, ordersAfter);
        Assert.NotNull(product);
        Assert.Equal(0, product.StockQuantity);
        Assert.Equal(0, product.ReservedStockQuantity);
        Assert.Equal(0, product.AvailableStockQuantity);
    }

    private async Task<Guid> CreateProductAsync(string name, decimal price)
    {
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Name = name,
            Price = price
        });

        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        return product!.Id;
    }

    private async Task AddStockAsync(Guid productId, int quantity)
    {
        var response = await _client.PostAsJsonAsync($"/products/{productId}/stock", new
        {
            Quantity = quantity
        });

        response.EnsureSuccessStatusCode();
    }

    private async Task<int> CountOrdersAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        return await dbContext.Orders.CountAsync();
    }

    private async Task<ProductResponse?> GetProductAsync(Guid productId)
    {
        var response = await _client.GetAsync($"/products/{productId}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductResponse>();
    }

    private sealed record CreatedOrderResponse(Guid Id);
}
