using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplyAI.Core.Entities;
using JobApplyAI.Mobile.Services;

namespace JobApplyAI.Mobile.ViewModels;

public partial class CoverLettersViewModel : ObservableObject
{
    private readonly NextRoleApiClient _api;

    public ObservableCollection<CoverLetter> Letters { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isOffline;

    public CoverLettersViewModel(NextRoleApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        IsOffline = false;
        try
        {
            var letters = await _api.GetCoverLettersAsync();
            Letters.Clear();
            foreach (var letter in letters.OrderByDescending(l => l.CreatedUtc))
            {
                Letters.Add(letter);
            }
            await OfflineCache.SaveAsync("cover-letters", letters);
        }
        catch (Exception ex)
        {
            var cached = await OfflineCache.LoadAsync<List<CoverLetter>>("cover-letters");
            if (cached is { Count: > 0 })
            {
                Letters.Clear();
                foreach (var letter in cached.OrderByDescending(l => l.CreatedUtc))
                {
                    Letters.Add(letter);
                }
                IsOffline = true;
                StatusMessage = "Showing your last-loaded cover letters - couldn't reach the API.";
            }
            else
            {
                StatusMessage = $"Couldn't load cover letters: {ex.Message}";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
