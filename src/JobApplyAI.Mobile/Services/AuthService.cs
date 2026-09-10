using System.IdentityModel.Tokens.Jwt;
using Microsoft.Identity.Client;

namespace JobApplyAI.Mobile.Services;

/// <summary>
/// Wraps MSAL.NET's public-client interactive sign-in against the same Entra External ID (CIAM)
/// tenant the Web app uses (a separate, public-client app registration is required - the Web
/// app's registration is confidential-client and cannot be reused here). When Entra isn't
/// configured (see <see cref="AppSettings.IsEntraConfigured"/>), the app simply stays in demo
/// mode - this mirrors the same "works out of the box, upgrades transparently once configured"
/// pattern used by the Web app and Functions API.
/// </summary>
public class AuthService
{
    private IPublicClientApplication? _app;
    private string? _cachedAccessToken;

    public bool IsSignedIn { get; private set; }
    public string? DisplayName { get; private set; }

    private IPublicClientApplication GetOrCreateApp()
    {
        if (_app is not null) return _app;

        _app = PublicClientApplicationBuilder
            .Create(AppSettings.EntraClientId)
            .WithAuthority(AppSettings.EntraAuthority)
            .WithRedirectUri(AppSettings.EntraRedirectUri)
            .Build();
        return _app;
    }

    /// <summary>Launches the system browser/broker for an interactive Entra External ID sign-in.
    /// On success, extracts the "oid" claim and uses it as the app's UserId going forward -
    /// the same partition-key identity scheme the Web app and Functions API already use.</summary>
    public async Task<bool> SignInAsync(CancellationToken ct = default)
    {
        if (!AppSettings.IsEntraConfigured)
        {
            return false;
        }

        try
        {
            var app = GetOrCreateApp();
            var scopes = new[] { $"{AppSettings.EntraClientId}/.default" };

            AuthenticationResult result;
            var accounts = await app.GetAccountsAsync();
            var existing = accounts.FirstOrDefault();
            try
            {
                result = existing is not null
                    ? await app.AcquireTokenSilent(scopes, existing).ExecuteAsync(ct)
                    : await AcquireInteractiveAsync(app, scopes, ct);
            }
            catch (MsalUiRequiredException)
            {
                result = await AcquireInteractiveAsync(app, scopes, ct);
            }

            _cachedAccessToken = result.AccessToken;
            ApplyResult(result);
            return true;
        }
        catch (Exception)
        {
            // Sign-in cancelled or failed - caller falls back to demo mode.
            return false;
        }
    }

    private static Task<AuthenticationResult> AcquireInteractiveAsync(
        IPublicClientApplication app, string[] scopes, CancellationToken ct)
        => app.AcquireTokenInteractive(scopes).ExecuteAsync(ct);

    private void ApplyResult(AuthenticationResult result)
    {
        IsSignedIn = true;
        DisplayName = result.Account?.Username;

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        var oid = token.Claims.FirstOrDefault(c => c.Type == "oid")?.Value
                  ?? token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (!string.IsNullOrWhiteSpace(oid))
        {
            AppSettings.UserId = oid;
        }
    }

    public async Task SignOutAsync()
    {
        if (_app is not null)
        {
            var accounts = await _app.GetAccountsAsync();
            foreach (var account in accounts)
            {
                await _app.RemoveAsync(account);
            }
        }

        _cachedAccessToken = null;
        IsSignedIn = false;
        DisplayName = null;
        AppSettings.UserId = AppSettings.DemoUserId;
    }

    /// <summary>Current cached access token, if signed in - attached as a Bearer token by
    /// <see cref="AuthHeaderHandler"/> on every API request.</summary>
    public string? AccessToken => IsSignedIn ? _cachedAccessToken : null;
}
