using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;
using Microsoft.Extensions.Logging;

namespace JobApplyAI.Infrastructure.JobSearch;

/// <summary>
/// Direct Seek integration remains a stub. Seek does not offer a public search API for
/// third-party consumer apps - only an official Partner/Employer API program requiring a
/// commercial agreement. Scraping seek.com.au would violate Seek's Website Terms of Use and
/// carries real legal risk, so this adapter intentionally returns no results rather than doing
/// that. Real AU/NZ coverage (including many listings that also appear on Seek, since employers
/// commonly cross-post) comes instead from <see cref="AdzunaJobSearchProvider"/> and
/// <see cref="JoobleJobSearchProvider"/>, both legitimate licensed aggregators with public free
/// developer APIs. If/when Seek partner API credentials are obtained, implement the real HTTP
/// calls here - no other code needs to change.
/// </summary>
public class SeekJobSearchProvider : IJobSearchProvider
{
    private readonly ILogger<SeekJobSearchProvider> _logger;

    public string ProviderName => "seek";

    public SeekJobSearchProvider(ILogger<SeekJobSearchProvider> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<JobPosting>> SearchAsync(
        string keywords, string location, Region region, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "SeekJobSearchProvider is a stub - no partner API credentials configured. " +
            "Returning empty results for keywords '{Keywords}' in {Location}, {Region}.",
            keywords, location, region);

        return Task.FromResult<IReadOnlyList<JobPosting>>(Array.Empty<JobPosting>());
    }
}
