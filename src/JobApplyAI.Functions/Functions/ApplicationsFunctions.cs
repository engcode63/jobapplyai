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

public class ApplicationsFunctions
{
    private readonly ApplicationTrackingService _tracking;
    private readonly IJobApplicationRepository _applications;
    private readonly IResumeRepository _resumes;
    private readonly IJobPostingRepository _jobPostings;
    private readonly ResumeTailoringService _tailoring;
    private readonly CoverLetterService _coverLetters;

    public ApplicationsFunctions(
        ApplicationTrackingService tracking,
        IJobApplicationRepository applications,
        IResumeRepository resumes,
        IJobPostingRepository jobPostings,
        ResumeTailoringService tailoring,
        CoverLetterService coverLetters)
    {
        _tracking = tracking;
        _applications = applications;
        _resumes = resumes;
        _jobPostings = jobPostings;
        _tailoring = tailoring;
        _coverLetters = coverLetters;
    }

    [Function("Applications_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userId}/applications")] HttpRequestData req,
        string userId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var applications = await _tracking.GetBoardAsync(userId);
        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.OK, applications);
    }

    [Function("Applications_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/{userId}/applications")] HttpRequestData req,
        string userId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var payload = await JsonSerializer.DeserializeAsync<CreateApplicationRequest>(req.Body);
        if (payload is null || string.IsNullOrWhiteSpace(payload.JobPostingId))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var created = await _tracking.CreateAsync(userId, payload.JobPostingId);
        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.Created, created);
    }

    [Function("Applications_ChangeStatus")]
    public async Task<HttpResponseData> ChangeStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "users/{userId}/applications/{applicationId}/status")]
        HttpRequestData req,
        string userId,
        string applicationId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var payload = await JsonSerializer.DeserializeAsync<ChangeStatusRequest>(req.Body);
        var application = await _applications.GetAsync(userId, applicationId);
        if (application is null || payload is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var updated = await _tracking.ChangeStatusAsync(application, payload.Status);
        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.OK, updated);
    }

    [Function("Applications_TailorResume")]
    public async Task<HttpResponseData> TailorResume(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/{userId}/applications/{applicationId}/tailor-resume")]
        HttpRequestData req,
        string userId,
        string applicationId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var application = await _applications.GetAsync(userId, applicationId);
        if (application is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var job = await _jobPostings.GetAsync(userId, application.JobPostingId);
        var resumes = await _resumes.ListByUserAsync(userId);
        var master = resumes.FirstOrDefault(r => r.IsMaster);
        if (job is null || master is null) return req.CreateResponse(HttpStatusCode.BadRequest);

        var tailored = await _tailoring.TailorResumeAsync(master, job);
        application.TailoredResumeId = tailored.Id;
        await _tracking.ChangeStatusAsync(application, application.Status);

        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.OK, tailored);
    }

    [Function("Applications_GenerateCoverLetter")]
    public async Task<HttpResponseData> GenerateCoverLetter(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/{userId}/applications/{applicationId}/cover-letter")]
        HttpRequestData req,
        string userId,
        string applicationId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var application = await _applications.GetAsync(userId, applicationId);
        if (application is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var job = await _jobPostings.GetAsync(userId, application.JobPostingId);
        var resumes = await _resumes.ListByUserAsync(userId);
        var resume = resumes.FirstOrDefault(r => r.Id == application.TailoredResumeId)
                     ?? resumes.FirstOrDefault(r => r.IsMaster);
        if (job is null || resume is null) return req.CreateResponse(HttpStatusCode.BadRequest);

        var letter = await _coverLetters.GenerateAsync(resume, job);
        application.CoverLetterId = letter.Id;
        await _tracking.ChangeStatusAsync(application, application.Status);

        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.OK, letter);
    }

    private record CreateApplicationRequest(string JobPostingId);
    private record ChangeStatusRequest(ApplicationStatus Status);
}
