namespace JobApplyAI.Mobile.Services;

/// <summary>Attaches a Bearer access token to every outgoing API request when the user is signed
/// in via Entra External ID; does nothing in demo mode (Functions accepts anonymous "demo-user"
/// requests when EntraExternalId:RequireAuthentication is false server-side).</summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly AuthService _authService;

    public AuthHeaderHandler(AuthService authService)
    {
        _authService = authService;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_authService.AccessToken is { } token)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
