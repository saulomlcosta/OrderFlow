using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrderFlow.IntegrationTests.Infrastructure;

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string AuthenticationScheme = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.Authorization.Equals(AuthenticationScheme))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var subject = Request.Headers["X-Test-Subject"].Count > 0
            ? Request.Headers["X-Test-Subject"].ToString()
            : "test-user-subject";

        var claims = new List<Claim>
        {
            new("sub", subject),
            new(ClaimTypes.NameIdentifier, subject),
            new(ClaimTypes.Name, "test-user")
        };

        var roles = Request.Headers["X-Test-Roles"].Count > 0
            ? Request.Headers["X-Test-Roles"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
            : ["administrator"];

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.Trim())));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationScheme));
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
