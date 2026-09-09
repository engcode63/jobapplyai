using System.Net;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Functions.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace JobApplyAI.Functions.Functions;

/// <summary>Read-only cover letter listing API - generation itself happens via
/// <see cref="ApplicationsFunctions.GenerateCoverLetter"/>, tied to a specific application.</summary>
public class CoverLettersFunctions
{
    private readonly ICoverLetterRepository _coverLetters;

    public CoverLettersFunctions(ICoverLetterRepository coverLetters)
    {
        _coverLetters = coverLetters;
    }

    [Function("CoverLetters_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userId}/cover-letters")] HttpRequestData req,
        string userId,
        FunctionContext context)
    {
        if (await context.AuthorizeUserAsync(req, userId) is { } denied) return denied;

        var letters = await _coverLetters.ListByUserAsync(userId);
        return await ResumesFunctions.WriteJsonAsync(req, HttpStatusCode.OK, letters);
    }
}
