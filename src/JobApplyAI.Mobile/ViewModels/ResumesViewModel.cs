using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplyAI.Core.Entities;
using JobApplyAI.Mobile.Services;

namespace JobApplyAI.Mobile.ViewModels;

public partial class ResumesViewModel : ObservableObject
{
    private readonly NextRoleApiClient _api;

    public ObservableCollection<Resume> Resumes { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ResumesViewModel(NextRoleApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var resumes = await _api.GetResumesAsync();
            Resumes.Clear();
            foreach (var resume in resumes.OrderByDescending(r => r.CreatedUtc))
            {
                Resumes.Add(resume);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't load resumes: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Uploads a resume file picked via <see cref="FilePicker"/> as the user's master resume.</summary>
    [RelayCommand]
    public async Task UploadAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a resume (PDF or text)"
            });
            if (file is null) return;

            IsBusy = true;
            await using var stream = await file.OpenReadAsync();
            var uploaded = await _api.UploadResumeAsync(file.FileName, stream);
            if (uploaded is not null)
            {
                Resumes.Insert(0, uploaded);
                StatusMessage = "Resume uploaded successfully.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Upload failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
