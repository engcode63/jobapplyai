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
/// Real, working AU/NZ job search via the Jooble API (https://jooble.org/api/about) - a
/// second legitimate job aggregator (free API key) used alongside Adzuna to widen coverage
/// across Australian/NZ employer sites and job boards without scraping anyone's site directly.
/// See AdzunaJobSearchProvider's remarks on why Seek/LinkedIn are deliberately not scraped.
/// </summary>
public class JoobleJobSearchProvider : IJobSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly JoobleOptions _options;
    private readonly ILogger<JoobleJobSearchProvider> _logger;

    public string ProviderName => "jooble";

    public JoobleJobSearchProvider(HttpClient httpClient, IOptions<JoobleOptions> options, ILogger<JoobleJobSearchProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobPosting>> SearchAsync(
        string keywords, string location, Region region, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || _options.ApiKey.Contains('<'))
        {
            _logger.LogWarning(
                "Jooble is not configured (Jooble:ApiKey missing) - skipping. " +
                "Register a free key at https://jooble.org/api/about to enable real results.");
            return Array.Empty<JobPosting>();
        }

        // Jooble exposes country-specific subdomains rather than a country query parameter.
        var subdomain = region == Region.NewZealand ? "nz" : "au";
        var url = $"https://{subdomain}.jooble.org/api/{Uri.EscapeDataString(_options.ApiKey)}";

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                url, new JoobleSearchRequest(keywords, location), ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<JoobleSearchResponse>(cancellationToken: ct);
            if (payload?.Jobs is null)
            {
                return Array.Empty<JobPosting>();
            }

            return payload.Jobs.Select(j => new JobPosting
            {
                Title = j.Title ?? string.Empty,
                Company = j.Company ?? string.Empty,
                Location = j.Location ?? location,
                Region = region,
                Description = j.Snippet ?? string.Empty,
                Source = "jooble",
                SourceUrl = j.Link
            }).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Jooble search failed for keywords '{Keywords}' in {Location}, {Region}.", keywords, location, region);
            return Array.Empty<JobPosting>();
        }
    }

    private sealed record JoobleSearchRequest(
        [property: JsonPropertyName("keywords")] string Keywords,
        [property: JsonPropertyName("location")] string Location);

    private sealed class JoobleSearchResponse
    {
        [JsonPropertyName("jobs")]
        public List<JoobleJob>? Jobs { get; set; }
    }

    private sealed class JoobleJob
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("snippet")]
        public string? Snippet { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }
    }
}
