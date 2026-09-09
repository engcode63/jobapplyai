namespace JobApplyAI.Core.Entities;

/// <summary>
/// An AI-driven mock interview session tied to a job posting.
/// Cosmos DB partition key: /userId
/// </summary>
public class InterviewSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; } = string.Empty;

    public string JobPostingId { get; set; } = string.Empty;

    public List<InterviewExchange> Exchanges { get; set; } = new();

    public string? OverallFeedback { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool IsComplete { get; set; }
}

public class InterviewExchange
{
    public string Question { get; set; } = string.Empty;
    public string? UserAnswer { get; set; }
    public string? AiFeedback { get; set; }
    public DateTimeOffset AskedUtc { get; set; } = DateTimeOffset.UtcNow;
}
