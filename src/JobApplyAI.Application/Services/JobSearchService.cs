using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;
using Microsoft.Extensions.Logging;

namespace JobApplyAI.Application.Services;

/// <summary>Fans out a search to all configured AU/NZ job board providers and merges results.</summary>
public class JobSearchService
{
    private readonly IEnumerable<IJobSearchProvider> _providers;
    private readonly ILogger<JobSearchService> _logger;

    public JobSearchService(IEnumerable<IJobSearchProvider> providers, ILogger<JobSearchService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    /// <summary>
    /// Runs every configured provider in parallel and merges whatever comes back. A single
    /// provider that throws (timeout, outage, bad credentials, etc.) is logged and treated as
    /// "no results from that provider" rather than failing the entire search - a Seek/Adzuna
    /// blip shouldn't mean the user gets a blank results page when other providers are healthy.
    /// </summary>
    public async Task<IReadOnlyList<JobPosting>> SearchAsync(
        string keywords, string location, Region region, CancellationToken ct = default)
    {
        var tasks = _providers.Select(p => SearchProviderSafelyAsync(p, keywords, location, region, ct));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private async Task<IReadOnlyList<JobPosting>> SearchProviderSafelyAsync(
        IJobSearchProvider provider, string keywords, string location, Region region, CancellationToken ct)
    {
        try
        {
            return await provider.SearchAsync(keywords, location, region, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex, "Job search provider '{Provider}' failed - excluding it from results.",
                provider.ProviderName);
            return Array.Empty<JobPosting>();
        }
    }
}
