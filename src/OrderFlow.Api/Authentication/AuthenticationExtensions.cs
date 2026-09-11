using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace OrderFlow.Api.Authentication;

internal static class AuthenticationExtensions
{
    internal static IServiceCollection AddOrderFlowAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication authority is not configured.");
        var audience = configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Authentication audience is not configured.");
        var requireHttpsMetadata = configuration.GetValue("Authentication:RequireHttpsMetadata", true);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(OrderFlowPolicies.Administrator, policy =>
                policy.RequireRole(OrderFlowRoles.Administrator));

        return services;
    }
}
