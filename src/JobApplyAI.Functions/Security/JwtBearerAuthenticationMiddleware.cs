using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using HttpRequestData = Microsoft.Azure.Functions.Worker.Http.HttpRequestData;

namespace JobApplyAI.Functions.Security;

/// <summary>
/// Validates Entra External ID (CIAM) JWT bearer access tokens on every HTTP-triggered function
/// invocation. Replaces the previous "AuthorizationLevel.Anonymous everywhere" scaffolding gap.
///
/// On success, the caller's object id ("oid" claim) is stashed in
/// <see cref="FunctionContext.Items"/> under <see cref="AuthenticatedUserItemKey"/> so downstream
/// function methods can retrieve it via <see cref="AuthenticatedUserAccessor"/> instead of
/// trusting a client-supplied "userId" route segment.
///
/// On failure (missing/invalid/expired token), the pipeline short-circuits and returns 401
/// without invoking the target function.
/// </summary>
public sealed class JwtBearerAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    public const string AuthenticatedUserItemKey = "AuthenticatedUserId";

    private readonly EntraExternalIdOptions _options;
    private readonly ILogger<JwtBearerAuthenticationMiddleware> _logger;
    private readonly ConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;

    public JwtBearerAuthenticationMiddleware(
        IOptions<EntraExternalIdOptions> options,
        ILogger<JwtBearerAuthenticationMiddleware> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.Authority))
        {
            var metadataAddress = $"{_options.Authority.TrimEnd('/')}/.well-known/openid-configuration";
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever());
        }
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();

        // Non-HTTP triggers (timers, queue, etc.) are not subject to bearer-token auth.
        if (httpRequestData is null)
        {
            await next(context);
            return;
        }

        // The health probe is intentionally anonymous: Azure Container Apps/App Gateway/Front
        // Door liveness checks can't present a user bearer token, and the endpoint itself
        // exposes no user data.
        if (httpRequestData.Url.AbsolutePath.TrimEnd('/').EndsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!_options.RequireAuthentication)
        {
            _logger.LogWarning(
                "EntraExternalId:RequireAuthentication is false - accepting request as demo-user. " +
                "This must never be enabled in a deployed environment.");
            context.Items[AuthenticatedUserItemKey] = "demo-user";
            await next(context);
            return;
        }

        if (_configurationManager is null)
        {
            _logger.LogError(
                "EntraExternalId:Authority is not configured; rejecting all requests. " +
                "Set EntraExternalId:Authority/Audience in application configuration.");
            await WriteUnauthorizedAsync(context, httpRequestData, "Authentication is not configured on this API.");
            return;
        }

        var token = ExtractBearerToken(httpRequestData);
        if (token is null)
        {
            await WriteUnauthorizedAsync(context, httpRequestData, "Missing bearer token.");
            return;
        }

        try
        {
            var openIdConfig = await _configurationManager.GetConfigurationAsync(context.CancellationToken);

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = openIdConfig.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);

            var userId = principal.FindFirst("oid")?.Value
                          ?? principal.FindFirst(ClaimTypesOid)?.Value
                          ?? principal.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                await WriteUnauthorizedAsync(context, httpRequestData, "Token is missing an object identifier claim.");
                return;
            }

            context.Items[AuthenticatedUserItemKey] = userId;
            await next(context);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Bearer token failed validation.");
            await WriteUnauthorizedAsync(context, httpRequestData, "Invalid or expired token.");
        }
    }

    // Fully-qualified XML SOAP claim type Entra sometimes maps "oid" to when using certain
    // token handlers/policies - kept as a fallback alongside the short "oid" claim name.
    private const string ClaimTypesOid = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    private static string? ExtractBearerToken(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues("Authorization", out var values))
        {
            return null;
        }

        var header = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return header["Bearer ".Length..].Trim();
    }

    private static async Task WriteUnauthorizedAsync(FunctionContext context, HttpRequestData request, string message)
    {
        var response = request.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(new { error = message });
        context.GetInvocationResult().Value = response;
    }
}
