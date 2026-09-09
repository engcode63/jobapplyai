using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplyAI.Mobile.Services;

namespace JobApplyAI.Mobile.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly NextRoleApiClient _api;

    [ObservableProperty] private string _apiBaseUrl;
    [ObservableProperty] private string _connectionStatus = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public SettingsViewModel(NextRoleApiClient api)
    {
        _api = api;
        _apiBaseUrl = AppSettings.ApiBaseUrl;
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
}
