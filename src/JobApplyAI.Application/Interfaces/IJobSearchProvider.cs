using JobApplyAI.Core.Entities;

namespace JobApplyAI.Application.Interfaces;

/// <summary>Adapter over an external AU/NZ job board (Seek, Indeed, etc). Implementations are pluggable
/// since these providers typically require commercial/partner API access.</summary>
public interface IJobSearchProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<JobPosting>> SearchAsync(
        string keywords, string location, Core.Enums.Region region, CancellationToken ct = default);
}
