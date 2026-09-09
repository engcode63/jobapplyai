namespace JobApplyAI.Infrastructure.JobSearch;

/// <summary>
/// Configuration for the Adzuna job search API (https://developer.adzuna.com/), a legitimate,
/// publicly documented job aggregator with a free-tier developer API covering Australia and
/// New Zealand. Adzuna aggregates listings (under licence) from thousands of employer sites and
/// job boards, so this is how "other Australian/NZ job sites" get covered without scraping any
/// individual site directly.
/// </summary>
public class AdzunaOptions
{
    public const string SectionName = "Adzuna";

    /// <summary>Adzuna "Application ID", obtained free at https://developer.adzuna.com/.</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Adzuna "Application Key", obtained alongside the App ID.</summary>
    public string AppKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.adzuna.com/v1/api";

    public int ResultsPerPage { get; set; } = 20;
}
