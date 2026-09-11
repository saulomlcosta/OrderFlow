using System.Net;
using System.Net.Http.Json;
using OrderFlow.Api.Products;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Products;

[Collection(nameof(OrderFlowApiCollection))]
public class ProductEndpointsTests(OrderFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private readonly HttpClient _client = factory.CreateAdministratorClient();

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
        Assert.Equal(0, product.ReservedStockQuantity);
        Assert.Equal(0, product.AvailableStockQuantity);
    }

    [Fact]
    public async Task ListProducts_ReturnsPublicCatalogOrderedByName()
    {
        await CreateProductAsync("Monitor", 1200m);
        await CreateProductAsync("Keyboard", 500m);

        using var anonymousClient = Factory.CreateClient();
        var response = await anonymousClient.GetAsync("/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<ProductResponse[]>();

        Assert.NotNull(products);
        Assert.Equal(["Keyboard", "Monitor"], products.Select(x => x.Name));
    }

    [Theory]
    [InlineData("/products")]
    [InlineData("/products/00000000-0000-0000-0000-000000000000/stock")]
    public async Task AdministrativeProductCommands_WithoutAuthentication_ReturnUnauthorized(string endpoint)
    {
        using var anonymousClient = Factory.CreateClient();
        var response = await anonymousClient.PostAsJsonAsync(endpoint, new
        {
            Name = "Unauthorized Product",
            Price = 10m,
            Quantity = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AsCustomer_ReturnsForbidden()
    {
        using var customerClient = Factory.CreateClient().AsCustomer();
        var response = await customerClient.PostAsJsonAsync("/products", new
        {
            Name = "Customer Product",
            Price = 10m
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
        Assert.Equal(0, product.ReservedStockQuantity);
        Assert.Equal(7, product.AvailableStockQuantity);
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
