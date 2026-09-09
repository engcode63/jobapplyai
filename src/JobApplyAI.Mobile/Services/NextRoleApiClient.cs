using System.Net.Http.Json;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;
using JobRegion = JobApplyAI.Core.Enums.Region;

namespace JobApplyAI.Mobile.Services;

/// <summary>
/// Typed client for the JobApplyAI.Functions HTTP API. Reads the base URL from
/// <see cref="AppSettings.ApiBaseUrl"/> on every call (rather than a fixed HttpClient.BaseAddress)
/// so changing the Settings page takes effect immediately without restarting the app.
/// </summary>
public class NextRoleApiClient
{
    private readonly HttpClient _http;

    public NextRoleApiClient(HttpClient http)
    {
        _http = http;
    }

    private string BaseUrl => AppSettings.ApiBaseUrl.TrimEnd('/');
    private string UserId => AppSettings.UserId;

    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync($"{BaseUrl}/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ---------- Resumes ----------

    public async Task<List<Resume>> GetResumesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<Resume>>($"{BaseUrl}/users/{UserId}/resumes", ct) ?? new();

    public async Task<Resume?> UploadResumeAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        using var streamContent = new StreamContent(content);
        streamContent.Headers.Add("X-File-Name", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/users/{UserId}/resumes")
        {
            Content = streamContent
        };
        request.Headers.Add("X-File-Name", fileName);
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Resume>(cancellationToken: ct);
    }

    // ---------- Job postings / search ----------

    public async Task<List<JobPosting>> GetJobPostingsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<JobPosting>>($"{BaseUrl}/users/{UserId}/job-postings", ct) ?? new();

    public async Task<List<JobPosting>> SearchJobsAsync(string keywords, string location, JobRegion region, CancellationToken ct = default)
    {
        var query = $"keywords={Uri.EscapeDataString(keywords)}&location={Uri.EscapeDataString(location)}&region={region}";
        return await _http.GetFromJsonAsync<List<JobPosting>>($"{BaseUrl}/job-postings/search?{query}", ct) ?? new();
    }

    public async Task<JobPosting?> CreateJobPostingAsync(JobPosting posting, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"{BaseUrl}/users/{UserId}/job-postings", posting, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JobPosting>(cancellationToken: ct);
    }

    // ---------- Applications ----------

    public async Task<List<JobApplication>> GetApplicationsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<JobApplication>>($"{BaseUrl}/users/{UserId}/applications", ct) ?? new();

    public async Task<JobApplication?> CreateApplicationAsync(string jobPostingId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"{BaseUrl}/users/{UserId}/applications", new { JobPostingId = jobPostingId }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JobApplication>(cancellationToken: ct);
    }

    public async Task<JobApplication?> ChangeApplicationStatusAsync(
        string applicationId, ApplicationStatus status, CancellationToken ct = default)
    {
        using var response = await _http.PatchAsJsonAsync(
            $"{BaseUrl}/users/{UserId}/applications/{applicationId}/status", new { Status = status }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JobApplication>(cancellationToken: ct);
    }

    public async Task<Resume?> TailorResumeAsync(string applicationId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync(
            $"{BaseUrl}/users/{UserId}/applications/{applicationId}/tailor-resume", content: null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Resume>(cancellationToken: ct);
    }

    public async Task<CoverLetter?> GenerateCoverLetterAsync(string applicationId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync(
            $"{BaseUrl}/users/{UserId}/applications/{applicationId}/cover-letter", content: null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CoverLetter>(cancellationToken: ct);
    }

    // ---------- Cover letters ----------

    public async Task<List<CoverLetter>> GetCoverLettersAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CoverLetter>>($"{BaseUrl}/users/{UserId}/cover-letters", ct) ?? new();

    // ---------- Interview prep ----------

    public async Task<InterviewSession?> StartInterviewAsync(string jobPostingId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"{BaseUrl}/users/{UserId}/interview-sessions", new { JobPostingId = jobPostingId }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InterviewSession>(cancellationToken: ct);
    }

    public async Task<InterviewSession?> SubmitInterviewAnswerAsync(
        string sessionId, string answer, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"{BaseUrl}/users/{UserId}/interview-sessions/{sessionId}/answers", new { Answer = answer }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InterviewSession>(cancellationToken: ct);
    }
}
