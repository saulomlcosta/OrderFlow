using System.Net;
using System.Net.Http.Headers;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Authentication;

[Collection(nameof(OrderFlowApiCollection))]
public sealed class AdministrativeAuthorizationTests : IntegrationTestBase
{
    private readonly OrderFlowApiFactory _factory;

    public AdministrativeAuthorizationTests(OrderFlowApiFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListCheckouts_WithoutIdentity_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/checkouts?status=active");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListCheckouts_WithCustomerRole_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        client.DefaultRequestHeaders.Add("X-Test-Roles", "customer");

        var response = await client.GetAsync("/checkouts?status=active");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListCheckouts_WithAdministratorRole_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.GetAsync("/checkouts?status=active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
