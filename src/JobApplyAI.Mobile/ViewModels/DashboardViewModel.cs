using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplyAI.Core.Entities;
using JobApplyAI.Mobile.Services;

namespace JobApplyAI.Mobile.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly NextRoleApiClient _api;

    [ObservableProperty] private int _resumeCount;
    [ObservableProperty] private int _applicationCount;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public DashboardViewModel(NextRoleApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var resumes = await _api.GetResumesAsync();
            var applications = await _api.GetApplicationsAsync();
            ResumeCount = resumes.Count(r => r.IsMaster);
            ApplicationCount = applications.Count;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't reach the API: {ex.Message}. Check the API URL in Settings.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
