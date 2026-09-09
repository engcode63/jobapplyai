using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplyAI.Core.Entities;
using JobApplyAI.Mobile.Services;

namespace JobApplyAI.Mobile.ViewModels;

public partial class InterviewPrepViewModel : ObservableObject
{
    private readonly NextRoleApiClient _api;

    public ObservableCollection<JobPosting> Postings { get; } = new();

    [ObservableProperty] private JobPosting? _selectedPosting;
    [ObservableProperty] private InterviewSession? _session;
    [ObservableProperty] private string _currentAnswer = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    /// <summary>The question the user should currently answer - the most recent exchange without an answer yet.</summary>
    public InterviewExchange? CurrentQuestion =>
        Session?.Exchanges.LastOrDefault(e => string.IsNullOrEmpty(e.UserAnswer));

    public InterviewPrepViewModel(NextRoleApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadPostingsAsync()
    {
        IsBusy = true;
        try
        {
            var postings = await _api.GetJobPostingsAsync();
            Postings.Clear();
            foreach (var posting in postings)
            {
                Postings.Add(posting);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't load job postings: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task StartAsync()
    {
        if (SelectedPosting is null)
        {
            StatusMessage = "Pick a saved job posting first.";
            return;
        }

        IsBusy = true;
        try
        {
            Session = await _api.StartInterviewAsync(SelectedPosting.Id);
            OnPropertyChanged(nameof(CurrentQuestion));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't start the interview: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SubmitAnswerAsync()
    {
        if (Session is null || string.IsNullOrWhiteSpace(CurrentAnswer)) return;

        IsBusy = true;
        try
        {
            Session = await _api.SubmitInterviewAnswerAsync(Session.Id, CurrentAnswer);
            CurrentAnswer = string.Empty;
            OnPropertyChanged(nameof(CurrentQuestion));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't submit your answer: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
