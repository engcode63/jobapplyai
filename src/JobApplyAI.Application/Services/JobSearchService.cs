using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;

namespace JobApplyAI.Application.Services;

/// <summary>Fans out a search to all configured AU/NZ job board providers and merges results.</summary>
public class JobSearchService
{
    private readonly IEnumerable<IJobSearchProvider> _providers;

    public JobSearchService(IEnumerable<IJobSearchProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<JobPosting>> SearchAsync(
        string keywords, string location, Region region, CancellationToken ct = default)
    {
        var tasks = _providers.Select(p => p.SearchAsync(keywords, location, region, ct));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }
}
