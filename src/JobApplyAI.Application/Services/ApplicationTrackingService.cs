using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;

namespace JobApplyAI.Application.Services;

/// <summary>CRUD + status-transition logic for the application tracking dashboard.</summary>
public class ApplicationTrackingService
{
    private readonly IJobApplicationRepository _applications;

    public ApplicationTrackingService(IJobApplicationRepository applications)
    {
        _applications = applications;
    }

    public Task<IReadOnlyList<JobApplication>> GetBoardAsync(string userId, CancellationToken ct = default)
        => _applications.ListByUserAsync(userId, ct);

    public async Task<JobApplication> CreateAsync(
        string userId, string jobPostingId, CancellationToken ct = default)
    {
        var application = new JobApplication { UserId = userId, JobPostingId = jobPostingId };
        application.StatusHistory.Add(new StatusHistoryEntry { Status = application.Status });
        return await _applications.UpsertAsync(application, ct);
    }

    public async Task<JobApplication> ChangeStatusAsync(
        JobApplication application, ApplicationStatus newStatus, CancellationToken ct = default)
    {
        application.Status = newStatus;
        application.UpdatedUtc = DateTimeOffset.UtcNow;
        application.StatusHistory.Add(new StatusHistoryEntry { Status = newStatus });
        return await _applications.UpsertAsync(application, ct);
    }
}
