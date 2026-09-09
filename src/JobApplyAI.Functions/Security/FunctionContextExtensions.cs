using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace JobApplyAI.Functions.Security;

/// <summary>
/// Helpers for function methods to retrieve the caller's identity (set by
/// <see cref="JwtBearerAuthenticationMiddleware"/>) and enforce that a client-supplied
/// "userId" route segment matches the authenticated caller, so no user can read or write
/// another user's data by editing the URL.
/// </summary>
public static class FunctionContextExtensions
{
    /// <summary>
    /// Returns the authenticated caller's user id (Entra object id), as resolved by the JWT
    /// bearer middleware. Returns null if, unexpectedly, no identity was attached (the
    /// middleware should always short-circuit unauthenticated requests before a function runs).
    /// </summary>
    public static string? GetAuthenticatedUserId(this FunctionContext context)
        => context.Items.TryGetValue(JwtBearerAuthenticationMiddleware.AuthenticatedUserItemKey, out var value)
            ? value as string
            : null;

    /// <summary>
    /// Verifies that <paramref name="routeUserId"/> (the "{userId}" segment from the request URL)
    /// matches the authenticated caller. Returns null when authorized, or a 401/403
    /// <see cref="HttpResponseData"/> to return immediately when the check fails.
    /// </summary>
    public static async Task<HttpResponseData?> AuthorizeUserAsync(
        this FunctionContext context, HttpRequestData request, string routeUserId)
    {
        var authenticatedUserId = context.GetAuthenticatedUserId();
        if (string.IsNullOrWhiteSpace(authenticatedUserId))
        {
            var unauthorized = request.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorized.WriteAsJsonAsync(new { error = "Not authenticated." });
            return unauthorized;
        }

        if (!string.Equals(authenticatedUserId, routeUserId, StringComparison.Ordinal))
        {
            var forbidden = request.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = "You may not access another user's data." });
            return forbidden;
        }

        return null;
    }
}
