namespace JobApplyAI.Core.Entities;

/// <summary>
/// An AI-generated cover letter for a specific job posting.
/// Cosmos DB partition key: /userId
/// </summary>
public class CoverLetter
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; } = string.Empty;

    public string JobPostingId { get; set; } = string.Empty;

    public string ResumeId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
