namespace JobApplyAI.Mobile.Services;

/// <summary>
/// User-editable connection settings, persisted via <see cref="Preferences"/> so they survive
/// app restarts. Defaults point at a local Functions host reachable from the Android emulator
/// (10.0.2.2 is the emulator's alias for the host machine's localhost) running in demo mode
/// (no Entra token required - see JwtBearerAuthenticationMiddleware).
/// </summary>
public static class AppSettings
{
    private const string ApiBaseUrlKey = "ApiBaseUrl";
    private const string UserIdKey = "UserId";

    /// <summary>
    /// Default API base URL. Android emulator: 10.0.2.2 reaches the host machine. iOS
    /// simulator/physical devices: use your machine's real LAN IP or a deployed Functions App
    /// URL instead - editable from the Settings page, no rebuild required.
    /// </summary>
    public const string DefaultApiBaseUrl = "http://10.0.2.2:7071/api";

    /// <summary>Matches CurrentUserService.DemoUserId on the Web/Functions side - the shared
    /// identity used everywhere while EntraExternalId:RequireAuthentication is left false.</summary>
    public const string DemoUserId = "demo-user";

    /// <summary>
    /// Entra External ID (CIAM) public-client app registration details for real sign-in.
    /// Placeholders here mean "not configured" - same convention used throughout the rest of
    /// the solution (Cosmos/AI Foundry/Entra on the Web side): the feature degrades gracefully
    /// (demo mode) rather than crashing when these are left blank.
    /// </summary>
    public const string PlaceholderClientId = "<your-mobile-app-registration-client-id>";
    public const string PlaceholderAuthority = "https://<your-tenant-name>.ciamlogin.com/<your-tenant-id>/v2.0";

    private const string EntraClientIdKey = "EntraClientId";
    private const string EntraAuthorityKey = "EntraAuthority";
    private const string EntraRedirectUriKey = "EntraRedirectUri";

    public static string EntraClientId
    {
        get => Preferences.Default.Get(EntraClientIdKey, PlaceholderClientId);
        set => Preferences.Default.Set(EntraClientIdKey, value);
    }

    public static string EntraAuthority
    {
        get => Preferences.Default.Get(EntraAuthorityKey, PlaceholderAuthority);
        set => Preferences.Default.Set(EntraAuthorityKey, value);
    }

    /// <summary>MSAL's default broker/system-browser redirect URI pattern for public clients:
    /// msal{ClientId}://auth - registered as a "Mobile and desktop applications" platform on the
    /// app registration.</summary>
    public static string EntraRedirectUri
    {
        get => Preferences.Default.Get(EntraRedirectUriKey, $"msal{EntraClientId}://auth");
        set => Preferences.Default.Set(EntraRedirectUriKey, value);
    }

    /// <summary>True once a real (non-placeholder) Client ID and Authority have been configured -
    /// same "IsConfigured" convention used for AdSense/Cosmos/AI Foundry elsewhere in this solution.</summary>
    public static bool IsEntraConfigured =>
        !string.IsNullOrWhiteSpace(EntraClientId) && !EntraClientId.Contains('<') &&
        !string.IsNullOrWhiteSpace(EntraAuthority) && !EntraAuthority.Contains('<');

    public static string ApiBaseUrl
    {
        get => Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
        set => Preferences.Default.Set(ApiBaseUrlKey, value);
    }

    /// <summary>
    /// The user id used in every "users/{userId}/..." API route. Until real Entra External ID
    /// sign-in is wired into this app, this is always <see cref="DemoUserId"/> - matching the
    /// same demo-mode fallback the Blazor Web app and Functions API already use.
    /// </summary>
    public static string UserId
    {
        get => Preferences.Default.Get(UserIdKey, DemoUserId);
        set => Preferences.Default.Set(UserIdKey, value);
    }
}
