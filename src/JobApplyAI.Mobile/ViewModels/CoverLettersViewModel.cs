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

    public CoverLettersViewModel(NextRoleApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var letters = await _api.GetCoverLettersAsync();
            Letters.Clear();
            foreach (var letter in letters.OrderByDescending(l => l.CreatedUtc))
            {
                Letters.Add(letter);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't load cover letters: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
