using System.Net;
using System.Text.Json;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Application.Services;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;
using JobApplyAI.Functions.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace JobApplyAI.Functions.Functions;

public class JobPostingsFunctions
{
    private readonly IJobPostingRepository _jobPostings;
    private readonly JobSearchService _searchService;

    public JobPostingsFunctions(IJobPostingRepository jobPostings, JobSearchService searchService)
    {
        _jobPostings = jobPostings;
        _searchService = searchService;
    }

    [Function("JobPostings_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userId}/job-postings")] HttpRequestData req,
        string userId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var postings = await _jobPostings.ListByUserAsync(userId);
        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.OK, postings);
    }

    [Function("JobPostings_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/{userId}/job-postings")] HttpRequestData req,
        string userId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var body = await JsonSerializer.DeserializeAsync<JobPosting>(req.Body) ?? new JobPosting();
        body.UserId = userId;
        var saved = await _jobPostings.UpsertAsync(body);
        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.Created, saved);
    }

    // No userId in the route - this searches a shared/public job catalog, not per-user data,
    // so no ownership check is required (authentication is still enforced by the middleware).
    [Function("JobPostings_Search")]
    public async Task<HttpResponseData> Search(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "job-postings/search")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var keywords = query["keywords"] ?? string.Empty;
        var location = query["location"] ?? string.Empty;
        var region = Enum.TryParse<Region>(query["region"], true, out var r) ? r : Region.Australia;

        var results = await _searchService.SearchAsync(keywords, location, region);
        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.OK, results);
    }
}
