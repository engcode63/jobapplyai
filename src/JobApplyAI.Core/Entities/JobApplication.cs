using JobApplyAI.Core.Enums;

namespace JobApplyAI.Core.Entities;

/// <summary>
/// Tracks a user's application to a specific job posting, including which
/// tailored resume/cover letter were used and the current pipeline status.
/// Cosmos DB partition key: /userId
/// </summary>
public class JobApplication
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; } = string.Empty;

    public string JobPostingId { get; set; } = string.Empty;

    public string? TailoredResumeId { get; set; }

    public string? CoverLetterId { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Saved;

    public List<StatusHistoryEntry> StatusHistory { get; set; } = new();

    public string? Notes { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class StatusHistoryEntry
{
    public ApplicationStatus Status { get; set; }
    public DateTimeOffset ChangedUtc { get; set; } = DateTimeOffset.UtcNow;
}
