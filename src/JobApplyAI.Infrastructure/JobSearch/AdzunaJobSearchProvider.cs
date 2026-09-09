using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobApplyAI.Infrastructure.JobSearch;

/// <summary>
/// Real, working AU/NZ job search via the Adzuna API (https://developer.adzuna.com/) - a
/// legitimate job aggregator with a public, free-tier developer API. Adzuna licenses listings
/// from a very large number of employer career sites and job boards across Australia and New
/// Zealand, so a single search here effectively covers "other job sites" beyond any one board.
///
/// Deliberately NOT implemented: scraping or unofficial API calls against seek.com.au or
/// linkedin.com. Both explicitly prohibit automated/programmatic data extraction in their Terms
/// of Service (Seek's Website Terms of Use; LinkedIn's User Agreement, reinforced by LinkedIn's
/// active enforcement against scrapers, e.g. the hiQ Labs litigation). Building a scraper for
/// either would create real legal exposure for you as the operator of this app. If direct Seek
/// coverage is required, the only compliant path is Seek's official Partner/Employer API
/// program (commercial agreement) - see README.
/// </summary>
public class AdzunaJobSearchProvider : IJobSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly AdzunaOptions _options;
    private readonly ILogger<AdzunaJobSearchProvider> _logger;

    public string ProviderName => "adzuna";

    public AdzunaJobSearchProvider(HttpClient httpClient, IOptions<AdzunaOptions> options, ILogger<AdzunaJobSearchProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobPosting>> SearchAsync(
        string keywords, string location, Region region, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppKey)
            || _options.AppId.Contains('<') || _options.AppKey.Contains('<'))
        {
            _logger.LogWarning(
                "Adzuna is not configured (Adzuna:AppId/AppKey missing) - skipping. " +
                "Register a free key at https://developer.adzuna.com/ to enable real results.");
            return Array.Empty<JobPosting>();
        }

        var country = region == Region.NewZealand ? "nz" : "au";
        var url = $"{_options.BaseUrl.TrimEnd('/')}/jobs/{country}/search/1"
                  + $"?app_id={Uri.EscapeDataString(_options.AppId)}"
                  + $"&app_key={Uri.EscapeDataString(_options.AppKey)}"
                  + $"&results_per_page={_options.ResultsPerPage}"
                  + $"&what={Uri.EscapeDataString(keywords)}"
                  + (string.IsNullOrWhiteSpace(location) ? string.Empty : $"&where={Uri.EscapeDataString(location)}")
                  + "&content-type=application/json";

        try
        {
            using var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<AdzunaSearchResponse>(cancellationToken: ct);
            if (payload?.Results is null)
            {
                return Array.Empty<JobPosting>();
            }

            return payload.Results.Select(r => new JobPosting
            {
                Title = r.Title ?? string.Empty,
                Company = r.Company?.DisplayName ?? string.Empty,
                Location = r.Location?.DisplayName ?? location,
                Region = region,
                Description = r.Description ?? string.Empty,
                Source = "adzuna",
                SourceUrl = r.RedirectUrl
            }).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Adzuna search failed for keywords '{Keywords}' in {Location}, {Region}.", keywords, location, region);
            return Array.Empty<JobPosting>();
        }
    }

    private sealed class AdzunaSearchResponse
    {
        [JsonPropertyName("results")]
        public List<AdzunaResult>? Results { get; set; }
    }

    private sealed class AdzunaResult
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("redirect_url")]
        public string? RedirectUrl { get; set; }

        [JsonPropertyName("company")]
        public AdzunaCompany? Company { get; set; }

        [JsonPropertyName("location")]
        public AdzunaLocation? Location { get; set; }
    }

    private sealed class AdzunaCompany
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }

    private sealed class AdzunaLocation
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}
