using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.IntegrationTests.Infrastructure;

internal static class TestAuthenticationExtensions
{
    internal static IServiceCollection AddTestAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.AuthenticationScheme,
                _ => { });

        return services;
    }

    internal static HttpClient AsAdministrator(
        this HttpClient client,
        string subject = "administrator-test-user")
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        return client;
    }

    internal static HttpClient AsCustomer(
        this HttpClient client,
        string subject = "customer-test-user")
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        client.DefaultRequestHeaders.Add("X-Test-Roles", "customer");
        return client;
    }
}
