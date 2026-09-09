namespace JobApplyAI.Infrastructure.Cosmos;

/// <summary>Cosmos DB connection/database/container configuration bound from app settings.</summary>
public class CosmosOptions
{
    public const string SectionName = "Cosmos";

    /// <summary>Cosmos account endpoint URI. Auth is via Managed Identity (DefaultAzureCredential) - no keys in config.</summary>
    public string AccountEndpoint { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = "JobApplyAI";

    public string UserProfilesContainer { get; set; } = "UserProfiles";
    public string ResumesContainer { get; set; } = "Resumes";
    public string JobPostingsContainer { get; set; } = "JobPostings";
    public string JobApplicationsContainer { get; set; } = "JobApplications";
    public string CoverLettersContainer { get; set; } = "CoverLetters";
    public string InterviewSessionsContainer { get; set; } = "InterviewSessions";
}
