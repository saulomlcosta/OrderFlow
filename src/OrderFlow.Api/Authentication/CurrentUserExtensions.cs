using System.Security.Claims;

namespace OrderFlow.Api.Authentication;

internal static class CurrentUserExtensions
{
    internal static string? GetSubject(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue("sub");
        return string.IsNullOrWhiteSpace(subject) ? null : subject;
    }

    internal static bool IsAdministrator(this ClaimsPrincipal principal) =>
        principal.IsInRole(OrderFlowRoles.Administrator);
}
