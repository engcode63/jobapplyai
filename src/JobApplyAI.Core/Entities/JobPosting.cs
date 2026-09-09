using JobApplyAI.Core.Enums;

namespace JobApplyAI.Core.Entities;

/// <summary>
/// A job posting saved/tracked by a user, sourced from a job board (Seek, Indeed AU/NZ, etc.)
/// or entered manually. Cosmos DB partition key: /userId
/// </summary>
public class JobPosting
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public Region Region { get; set; } = Region.Australia;

    public string Description { get; set; } = string.Empty;

    /// <summary>Which external source this posting came from (e.g. "seek", "indeed", "manual").</summary>
    public string Source { get; set; } = "manual";

    /// <summary>External URL to the original job ad, if sourced externally.</summary>
    public string? SourceUrl { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
