namespace JobApplyAI.Infrastructure.JobSearch;

/// <summary>
/// Configuration for the Jooble job search API (https://jooble.org/api/about), another
/// legitimate, publicly available job aggregator with a free developer API key, covering
/// Australia and New Zealand via country-specific subdomains.
/// </summary>
public class JoobleOptions
{
    public const string SectionName = "Jooble";

    /// <summary>API key, obtained free at https://jooble.org/api/about.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
