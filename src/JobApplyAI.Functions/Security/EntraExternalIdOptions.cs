namespace JobApplyAI.Functions.Security;

/// <summary>
/// Configuration for validating Entra External ID (CIAM) JWT bearer access tokens presented to
/// the Functions HTTP API. Bind from the "EntraExternalId" configuration section
/// (Functions app settings / local.settings.json / Azure App Settings in production).
/// </summary>
public sealed class EntraExternalIdOptions
{
    /// <summary>
    /// CIAM authority, e.g. "https://&lt;tenant-name&gt;.ciamlogin.com/&lt;tenant-id&gt;/v2.0".
    /// Used to discover the OpenID Connect metadata (issuer + signing keys) for token validation.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Expected "aud" claim on incoming access tokens - the Application ID URI or Client ID of
    /// the app registration that represents this Functions API (may be the same app registration
    /// as the Blazor Web client if it exposes an API scope, or a dedicated API app registration).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// When true (default), incoming requests without a valid bearer token are rejected with 401.
    /// Set to false only for local development against a Functions host with no Entra config yet,
    /// in which case requests fall back to an unauthenticated "demo-user" identity - never set
    /// this to false in a deployed environment.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;
}
