using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace JobApplyAI.Web.Services;

/// <summary>
/// Resolves the current user's stable identifier (Entra External ID 'oid' claim) for use as
/// the Cosmos DB partition key. Falls back to a fixed demo id when running without configured
/// authentication (e.g. local development before Entra External ID is wired up).
/// </summary>
public class CurrentUserService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public const string DemoUserId = "demo-user";

    public CurrentUserService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<string> GetUserIdAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var user = state.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var oid = user.FindFirst("oid")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(oid))
            {
                return oid;
            }
        }

        return DemoUserId;
    }
}
