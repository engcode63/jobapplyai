using System.Net;
using System.Text.Json;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Application.Services;
using JobApplyAI.Functions.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace JobApplyAI.Functions.Functions;

public class InterviewFunctions
{
    private readonly InterviewPrepService _interviewService;
    private readonly IJobPostingRepository _jobPostings;
    private readonly IInterviewSessionRepository _sessions;

    public InterviewFunctions(
        InterviewPrepService interviewService,
        IJobPostingRepository jobPostings,
        IInterviewSessionRepository sessions)
    {
        _interviewService = interviewService;
        _jobPostings = jobPostings;
        _sessions = sessions;
    }

    [Function("Interview_Start")]
    public async Task<HttpResponseData> Start(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/{userId}/interview-sessions")] HttpRequestData req,
        string userId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var payload = await JsonSerializer.DeserializeAsync<StartRequest>(req.Body);
        if (payload is null) return req.CreateResponse(HttpStatusCode.BadRequest);

        var job = await _jobPostings.GetAsync(userId, payload.JobPostingId);
        if (job is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var session = await _interviewService.StartSessionAsync(userId, job);
        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.Created, session);
    }

    [Function("Interview_SubmitAnswer")]
    public async Task<HttpResponseData> SubmitAnswer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/{userId}/interview-sessions/{sessionId}/answers")]
        HttpRequestData req,
        string userId,
        string sessionId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var payload = await JsonSerializer.DeserializeAsync<AnswerRequest>(req.Body);
        var session = await _sessions.GetAsync(userId, sessionId);
        if (session is null || payload is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var job = await _jobPostings.GetAsync(userId, session.JobPostingId);
        if (job is null) return req.CreateResponse(HttpStatusCode.BadRequest);

        var updated = await _interviewService.SubmitAnswerAsync(session, job, payload.Answer);
        var withNextQuestion = await _interviewService.NextQuestionAsync(updated, job);
        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.OK, withNextQuestion);
    }

    private record StartRequest(string JobPostingId);
    private record AnswerRequest(string Answer);
}
