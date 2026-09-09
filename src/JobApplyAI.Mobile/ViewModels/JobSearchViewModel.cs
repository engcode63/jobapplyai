using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplyAI.Core.Entities;
using JobApplyAI.Mobile.Services;
using JobRegion = JobApplyAI.Core.Enums.Region;

namespace JobApplyAI.Mobile.ViewModels;

public partial class JobSearchViewModel : ObservableObject
{
    private readonly NextRoleApiClient _api;

    public ObservableCollection<JobPosting> Results { get; } = new();
    public List<JobRegion> Regions { get; } = Enum.GetValues<JobRegion>().ToList();

    [ObservableProperty] private string _keywords = string.Empty;
    [ObservableProperty] private string _location = string.Empty;
    [ObservableProperty] private JobRegion _selectedRegion = JobRegion.Australia;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public JobSearchViewModel(NextRoleApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Keywords))
        {
            StatusMessage = "Enter a keyword (e.g. job title) to search.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var results = await _api.SearchJobsAsync(Keywords, Location, SelectedRegion);
            Results.Clear();
            foreach (var posting in results)
            {
                Results.Add(posting);
            }
            if (Results.Count == 0)
            {
                StatusMessage = "No matching jobs found - try different keywords.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Saves a search result to the user's tracked job postings so it can be turned into an application.</summary>
    [RelayCommand]
    public async Task SaveAsync(JobPosting posting)
    {
        try
        {
            await _api.CreateJobPostingAsync(posting);
            StatusMessage = $"Saved \"{posting.Title}\" to your job postings.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't save: {ex.Message}";
        }
    }
}
