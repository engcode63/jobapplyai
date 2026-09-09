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
