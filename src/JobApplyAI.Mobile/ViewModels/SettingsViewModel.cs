using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplyAI.Mobile.Services;

namespace JobApplyAI.Mobile.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly NextRoleApiClient _api;
    private readonly AuthService _auth;

    [ObservableProperty] private string _apiBaseUrl;
    [ObservableProperty] private string _connectionStatus = string.Empty;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _entraClientId;
    [ObservableProperty] private string _entraAuthority;
    [ObservableProperty] private string _authStatus = string.Empty;
    [ObservableProperty] private bool _isSignedIn;
    [ObservableProperty] private string? _signedInDisplayName;

    public SettingsViewModel(NextRoleApiClient api, AuthService auth)
    {
        _api = api;
        _auth = auth;
        _apiBaseUrl = AppSettings.ApiBaseUrl;
        _entraClientId = AppSettings.EntraClientId;
        _entraAuthority = AppSettings.EntraAuthority;
        _isSignedIn = auth.IsSignedIn;
        _signedInDisplayName = auth.DisplayName;
    }

    [RelayCommand]
    public void Save()
    {
        AppSettings.ApiBaseUrl = ApiBaseUrl.Trim().TrimEnd('/');
        ConnectionStatus = "Saved. Use \"Test connection\" to verify the API is reachable.";
    }

    [RelayCommand]
    public async Task TestConnectionAsync()
    {
        IsBusy = true;
        ConnectionStatus = "Checking...";
        try
        {
            Save();
            var reachable = await _api.CheckHealthAsync();
            ConnectionStatus = reachable
                ? "Connected! The API is reachable."
                : "Couldn't reach the API at that URL. Double-check it's running and the URL is correct.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ResetToDefault()
    {
        ApiBaseUrl = AppSettings.DefaultApiBaseUrl;
        Save();
    }

    /// <summary>Persists the Entra Client ID/Authority entered on this page - required before
    /// SignIn can succeed. Left as placeholders, sign-in stays disabled and the app remains in
    /// demo mode.</summary>
    [RelayCommand]
    public void SaveEntraConfig()
    {
        AppSettings.EntraClientId = EntraClientId.Trim();
        AppSettings.EntraAuthority = EntraAuthority.Trim();
        AuthStatus = AppSettings.IsEntraConfigured
            ? "Saved. Tap \"Sign in\" to authenticate."
            : "Saved, but this still looks like a placeholder - sign-in stays disabled until both fields have real values.";
    }

    [RelayCommand]
    public async Task SignInAsync()
    {
        SaveEntraConfig();
        if (!AppSettings.IsEntraConfigured)
        {
            AuthStatus = "Configure a real Client ID and Authority above before signing in.";
            return;
        }

        IsBusy = true;
        try
        {
            var success = await _auth.SignInAsync();
            IsSignedIn = _auth.IsSignedIn;
            SignedInDisplayName = _auth.DisplayName;
            AuthStatus = success
                ? $"Signed in as {_auth.DisplayName}."
                : "Sign-in was cancelled or failed - staying in demo mode.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SignOutAsync()
    {
        await _auth.SignOutAsync();
        IsSignedIn = false;
        SignedInDisplayName = null;
        AuthStatus = "Signed out - back to demo mode.";
    }
}
