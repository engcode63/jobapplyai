namespace JobApplyAI.Core.Entities;

/// <summary>
/// A resume owned by a user - either the original uploaded master resume,
/// or a version tailored for a specific job application.
/// Cosmos DB partition key: /userId
/// </summary>
public class Resume
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    /// <summary>Blob storage path of the original uploaded file (PDF/DOCX).</summary>
    public string? SourceBlobPath { get; set; }

    /// <summary>Raw extracted text used as input for AI processing.</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>Structured sections parsed out of the resume (best effort).</summary>
    public ResumeStructuredData? Structured { get; set; }

    /// <summary>True if this is the user's master/base resume; false if tailored for a specific job.</summary>
    public bool IsMaster { get; set; }

    /// <summary>If tailored, the JobPosting.Id this version targets.</summary>
    public string? TailoredForJobPostingId { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class ResumeStructuredData
{
    public string? Summary { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<WorkExperience> Experience { get; set; } = new();
    public List<Education> Education { get; set; } = new();
}

public class WorkExperience
{
    public string Company { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public List<string> Highlights { get; set; } = new();
}

public class Education
{
    public string Institution { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string? Year { get; set; }
}
