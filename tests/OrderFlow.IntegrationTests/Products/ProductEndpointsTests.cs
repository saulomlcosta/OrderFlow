using System.Net;
using System.Net.Http.Json;
using OrderFlow.Api.Products;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Products;

[Collection(nameof(OrderFlowApiCollection))]
public class ProductEndpointsTests(OrderFlowApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateProduct_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Name = "Laptop",
            Price = 3500m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);
        Assert.Equal("Laptop", product.Name);
        Assert.Equal(3500m, product.Price);
        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public async Task AddStock_ThenGetProduct_Succeeds()
    {
        var productId = await CreateProductAsync("Headset", 200m);

        var addStockResponse = await _client.PostAsJsonAsync($"/products/{productId}/stock", new
        {
            Quantity = 7
        });

        Assert.Equal(HttpStatusCode.OK, addStockResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/products/{productId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var product = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);
        Assert.Equal(productId, product.Id);
        Assert.Equal(7, product.StockQuantity);
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
}
