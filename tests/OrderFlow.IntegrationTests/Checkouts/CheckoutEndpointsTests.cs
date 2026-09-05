using System.Net;
using System.Net.Http.Json;
using OrderFlow.Api.Checkouts;
using OrderFlow.Api.Products;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Checkouts;

[Collection(nameof(OrderFlowApiCollection))]
public class CheckoutEndpointsTests(OrderFlowApiFactory factory)
{
    private readonly OrderFlowApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task StartCheckout_WithEnoughStock_ReservesQuantity()
    {
        _factory.ResetTime();
        var productId = await CreateProductAsync("Console", 2500m);
        await AddStockAsync(productId, 5);

        var response = await _client.PostAsJsonAsync("/checkouts", new
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

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        var product = await GetProductAsync(productId);

        Assert.NotNull(checkout);
        Assert.Equal("Active", checkout.Status);
        Assert.False(checkout.IsExpired);
        Assert.NotNull(product);
        Assert.Equal(5, product.StockQuantity);
        Assert.Equal(2, product.ReservedStockQuantity);
        Assert.Equal(3, product.AvailableStockQuantity);
    }

    [Fact]
    public async Task CancelCheckout_ReleasesReservedQuantity()
    {
        _factory.ResetTime();
        var productId = await CreateProductAsync("Chair", 500m);
        await AddStockAsync(productId, 4);

        var checkoutId = await StartCheckoutAsync(productId, 2);

        var response = await _client.PostAsync($"/checkouts/{checkoutId}/cancel", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        var product = await GetProductAsync(productId);

        Assert.NotNull(checkout);
        Assert.Equal("Cancelled", checkout.Status);
        Assert.False(checkout.IsExpired);
        Assert.NotNull(product);
        Assert.Equal(4, product.StockQuantity);
        Assert.Equal(0, product.ReservedStockQuantity);
        Assert.Equal(4, product.AvailableStockQuantity);
    }

    [Fact]
    public async Task CompleteCheckout_CreatesOrder_AndConsumesReservedQuantity()
    {
        _factory.ResetTime();
        var productId = await CreateProductAsync("Desk", 900m);
        await AddStockAsync(productId, 5);

        var checkoutId = await StartCheckoutAsync(productId, 2);

        var response = await _client.PostAsync($"/checkouts/{checkoutId}/complete", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var product = await GetProductAsync(productId);

        Assert.NotNull(product);
        Assert.Equal(3, product.StockQuantity);
        Assert.Equal(0, product.ReservedStockQuantity);
        Assert.Equal(3, product.AvailableStockQuantity);
    }

    [Fact]
    public async Task CancelCheckout_WhenReservationIsExpired_ReturnsValidationError()
    {
        _factory.ResetTime();
        var productId = await CreateProductAsync("Headphones", 300m);
        await AddStockAsync(productId, 2);
        var checkoutId = await StartCheckoutAsync(productId, 1);
        _factory.AdvanceTime(TimeSpan.FromMinutes(16));

        var response = await _client.PostAsync($"/checkouts/{checkoutId}/cancel", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var product = await GetProductAsync(productId);

        Assert.NotNull(product);
        Assert.Equal(2, product.StockQuantity);
        Assert.Equal(1, product.ReservedStockQuantity);
        Assert.Equal(1, product.AvailableStockQuantity);
    }

    [Fact]
    public async Task ExpireCheckout_ReleasesReservedQuantity_AndChangesStatus()
    {
        _factory.ResetTime();
        var productId = await CreateProductAsync("Speaker", 800m);
        await AddStockAsync(productId, 3);
        var checkoutId = await StartCheckoutAsync(productId, 2);
        _factory.AdvanceTime(TimeSpan.FromMinutes(16));

        var response = await _client.PostAsync($"/checkouts/{checkoutId}/expire", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        var product = await GetProductAsync(productId);

        Assert.NotNull(checkout);
        Assert.Equal("Expired", checkout.Status);
        Assert.True(checkout.IsExpired);
        Assert.NotNull(product);
        Assert.Equal(3, product.StockQuantity);
        Assert.Equal(0, product.ReservedStockQuantity);
        Assert.Equal(3, product.AvailableStockQuantity);
    }

    [Fact]
    public async Task ListExpiredCheckouts_ReturnsOperationallyExpiredCheckouts()
    {
        _factory.ResetTime();
        var productId = await CreateProductAsync("Tablet", 1500m);
        await AddStockAsync(productId, 2);
        var checkoutId = await StartCheckoutAsync(productId, 1);
        _factory.AdvanceTime(TimeSpan.FromMinutes(16));

        var response = await _client.GetAsync("/checkouts?status=expired");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var checkouts = await response.Content.ReadFromJsonAsync<List<CheckoutResponse>>();
        Assert.NotNull(checkouts);

        var checkout = Assert.Single(checkouts, item => item.Id == checkoutId);
        Assert.Equal("Active", checkout.Status);
        Assert.True(checkout.IsExpired);
        Assert.All(checkouts, item => Assert.True(item.IsExpired));
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

    private async Task<Guid> StartCheckoutAsync(Guid productId, int quantity)
    {
        var response = await _client.PostAsJsonAsync("/checkouts", new
        {
            Items = new[]
            {
                new
                {
                    ProductId = productId,
                    Quantity = quantity
                }
            }
        });

        response.EnsureSuccessStatusCode();

        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        return checkout!.Id;
    }

    private async Task<ProductResponse?> GetProductAsync(Guid productId)
    {
        var response = await _client.GetAsync($"/products/{productId}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductResponse>();
    }
}
