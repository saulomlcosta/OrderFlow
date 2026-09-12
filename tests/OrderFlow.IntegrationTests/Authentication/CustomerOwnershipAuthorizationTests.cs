using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Checkouts;
using OrderFlow.Api.Persistence;
using OrderFlow.Api.Products;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Authentication;

[Collection(nameof(OrderFlowApiCollection))]
public sealed class CustomerOwnershipAuthorizationTests(OrderFlowApiFactory factory)
    : IntegrationTestBase(factory)
{
    private const string OwnerSubject = "customer-owner";
    private readonly HttpClient _administrator = factory.CreateAdministratorClient();
    private readonly HttpClient _owner = factory.CreateCustomerClient(OwnerSubject);
    private readonly HttpClient _otherCustomer = factory.CreateCustomerClient("other-customer");

    [Fact]
    public async Task StartCheckout_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var anonymous = Factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/checkouts", new
        {
            Items = new[] { new { ProductId = Guid.NewGuid(), Quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_IsVisibleToOwnerAndAdministrator_ButHiddenFromAnotherCustomer()
    {
        var checkoutId = await CreateCheckoutAsync();

        var ownerResponse = await _owner.GetAsync($"/checkouts/{checkoutId}");
        var administratorResponse = await _administrator.GetAsync($"/checkouts/{checkoutId}");
        var otherCustomerResponse = await _otherCustomer.GetAsync($"/checkouts/{checkoutId}");

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, administratorResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherCustomerResponse.StatusCode);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var persistedSubject = await dbContext.Checkouts
            .Where(x => x.Id == checkoutId)
            .Select(x => x.CustomerSubject)
            .SingleAsync();

        Assert.Equal(OwnerSubject, persistedSubject);
    }

    [Fact]
    public async Task CancelCheckout_ByAnotherCustomer_ReturnsNotFoundAndPreservesOwnerOperation()
    {
        var checkoutId = await CreateCheckoutAsync();

        var otherCustomerResponse = await _otherCustomer.PostAsync(
            $"/checkouts/{checkoutId}/cancel",
            content: null);
        var ownerResponse = await _owner.PostAsync(
            $"/checkouts/{checkoutId}/cancel",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, otherCustomerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task CompleteCheckout_ByAdministratorIsHidden_ThenOwnerCreatesOwnedOrder()
    {
        var checkoutId = await CreateCheckoutAsync();

        var administratorCompletion = await _administrator.PostAsync(
            $"/checkouts/{checkoutId}/complete",
            content: null);
        var ownerCompletion = await _owner.PostAsync(
            $"/checkouts/{checkoutId}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, administratorCompletion.StatusCode);
        Assert.Equal(HttpStatusCode.Created, ownerCompletion.StatusCode);

        var created = await ownerCompletion.Content.ReadFromJsonAsync<CreatedOrderResponse>();
        var ownerRead = await _owner.GetAsync($"/orders/{created!.Id}");
        var administratorRead = await _administrator.GetAsync($"/orders/{created.Id}");
        var otherCustomerRead = await _otherCustomer.GetAsync($"/orders/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, ownerRead.StatusCode);
        Assert.Equal(HttpStatusCode.OK, administratorRead.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherCustomerRead.StatusCode);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var persistedSubject = await dbContext.Orders
            .Where(x => x.Id == created.Id)
            .Select(x => x.CustomerSubject)
            .SingleAsync();

        Assert.Equal(OwnerSubject, persistedSubject);
    }

    private async Task<Guid> CreateCheckoutAsync()
    {
        var productResponse = await _administrator.PostAsJsonAsync("/products", new
        {
            Name = $"Ownership Product {Guid.NewGuid():N}",
            Price = 100m
        });
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var stockResponse = await _administrator.PostAsJsonAsync($"/products/{product!.Id}/stock", new
        {
            Quantity = 2
        });
        stockResponse.EnsureSuccessStatusCode();

        var checkoutResponse = await _owner.PostAsJsonAsync("/checkouts", new
        {
            Items = new[] { new { ProductId = product.Id, Quantity = 1 } }
        });
        checkoutResponse.EnsureSuccessStatusCode();
        var checkout = await checkoutResponse.Content.ReadFromJsonAsync<CheckoutResponse>();

        return checkout!.Id;
    }

    private sealed record CreatedOrderResponse(Guid Id, Guid CheckoutId);
}
