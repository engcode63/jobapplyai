using System.Net;
using System.Text.Json;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;
using JobApplyAI.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace JobApplyAI.Functions.Functions;

/// <summary>
/// Resume upload/list API. Every route below still takes a "{userId}" route segment for URL
/// readability, but <see cref="FunctionContextExtensions.AuthorizeUserAsync"/> (populated by
/// <see cref="JwtBearerAuthenticationMiddleware"/>) enforces that it matches the caller's
/// authenticated Entra External ID identity - a request presenting a valid token for user A can
/// never read/write user B's route.
/// </summary>
public class ResumesFunctions
{
    private readonly IResumeRepository _resumes;
    private readonly IResumeParsingService _parsing;
    private readonly ILogger<ResumesFunctions> _logger;

    public ResumesFunctions(IResumeRepository resumes, IResumeParsingService parsing, ILogger<ResumesFunctions> logger)
    {
        _resumes = resumes;
        _parsing = parsing;
        _logger = logger;
    }

    [Function("Resumes_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userId}/resumes")] HttpRequestData req,
        string userId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var resumes = await _resumes.ListByUserAsync(userId);
        return await WriteJsonAsync(req, HttpStatusCode.OK, resumes);
    }

    [Function("Resumes_Upload")]
    public async Task<HttpResponseData> Upload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/{userId}/resumes")] HttpRequestData req,
        string userId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var fileName = req.Headers.TryGetValues("X-File-Name", out var values) ? values.First() : "resume.pdf";

        var (rawText, structured) = await _parsing.ParseAsync(req.Body, fileName);

        var resume = new Resume
        {
            UserId = userId,
            FileName = fileName,
            RawText = rawText,
            Structured = structured,
            IsMaster = true
        };

        var saved = await _resumes.UpsertAsync(resume);
        _logger.LogInformation("Resume {ResumeId} uploaded for user {UserId}", saved.Id, userId);
        return await WriteJsonAsync(req, HttpStatusCode.Created, saved);
    }

    internal static async Task<HttpResponseData> WriteJsonAsync<T>(HttpRequestData req, HttpStatusCode status, T body)
    {
        var response = req.CreateResponse(status);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(body));
        return response;
    }
}
