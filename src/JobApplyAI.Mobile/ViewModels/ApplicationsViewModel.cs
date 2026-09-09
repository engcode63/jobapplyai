using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;
using JobApplyAI.Mobile.Services;

namespace JobApplyAI.Mobile.ViewModels;

/// <summary>Display wrapper joining a JobApplication with its JobPosting details client-side,
/// since the API keeps them as separate resources linked only by JobPostingId.</summary>
public partial class ApplicationItem : ObservableObject
{
    public JobApplication Application { get; }
    public JobPosting? Posting { get; set; }

    public string Title => Posting?.Title ?? "(job posting unavailable)";
    public string Company => Posting?.Company ?? string.Empty;

    [ObservableProperty] private ApplicationStatus _status;

    public ApplicationItem(JobApplication application, JobPosting? posting)
    {
        Application = application;
        Posting = posting;
        _status = application.Status;
    }
}

public partial class ApplicationsViewModel : ObservableObject
{
    private readonly NextRoleApiClient _api;

    public ObservableCollection<ApplicationItem> Applications { get; } = new();
    public ObservableCollection<JobPosting> AvailablePostings { get; } = new();
    public List<ApplicationStatus> Statuses { get; } = Enum.GetValues<ApplicationStatus>().ToList();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ApplicationsViewModel(NextRoleApiClient api)
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
            var applications = await _api.GetApplicationsAsync();
            var postings = await _api.GetJobPostingsAsync();
            var postingsById = postings.ToDictionary(p => p.Id);

            Applications.Clear();
            AvailablePostings.Clear();

            var appliedPostingIds = applications.Select(a => a.JobPostingId).ToHashSet();

            foreach (var app in applications.OrderByDescending(a => a.UpdatedUtc))
            {
                postingsById.TryGetValue(app.JobPostingId, out var posting);
                Applications.Add(new ApplicationItem(app, posting));
            }

            foreach (var posting in postings.Where(p => !appliedPostingIds.Contains(p.Id)))
            {
                AvailablePostings.Add(posting);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't load applications: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ApplyAsync(JobPosting posting)
    {
        try
        {
            await _api.CreateApplicationAsync(posting.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't create application: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task AdvanceStatusAsync(ApplicationItem item)
    {
        var nextIndex = ((int)item.Status + 1) % Statuses.Count;
        var next = Statuses[nextIndex];
        try
        {
            var updated = await _api.ChangeApplicationStatusAsync(item.Application.Id, next);
            if (updated is not null)
            {
                item.Status = updated.Status;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't update status: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task TailorResumeAsync(ApplicationItem item)
    {
        try
        {
            var resume = await _api.TailorResumeAsync(item.Application.Id);
            StatusMessage = resume is not null
                ? "Tailored resume generated - check the Resumes tab."
                : "Couldn't generate a tailored resume.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Tailoring failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task GenerateCoverLetterAsync(ApplicationItem item)
    {
        try
        {
            var letter = await _api.GenerateCoverLetterAsync(item.Application.Id);
            StatusMessage = letter is not null
                ? "Cover letter generated - check the Cover Letters tab."
                : "Couldn't generate a cover letter.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Generation failed: {ex.Message}";
        }
    }
}
