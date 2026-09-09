using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;
using Microsoft.Extensions.Logging;

namespace JobApplyAI.Application.Services;

/// <summary>
/// Generates a job-tailored resume by asking the AI model to rewrite the user's
/// master resume to better match a specific job posting, without fabricating experience.
/// </summary>
public class ResumeTailoringService
{
    private readonly IAiCompletionService _ai;
    private readonly IResumeRepository _resumes;
    private readonly ILogger<ResumeTailoringService> _logger;

    public ResumeTailoringService(
        IAiCompletionService ai,
        IResumeRepository resumes,
        ILogger<ResumeTailoringService> logger)
    {
        _ai = ai;
        _resumes = resumes;
        _logger = logger;
    }

    private const string SystemPrompt =
        """
        You are an expert resume writer specialising in the Australian and New Zealand job markets.
        Rewrite the candidate's resume to better align with the target job posting.
        Rules:
        - Never invent employers, job titles, dates, qualifications, or skills the candidate does not have.
        - Reorder and emphasise existing, genuine experience/skills that match the job posting.
        - Use clear, concise, achievement-oriented bullet points (quantify impact where the source material allows).
        - Keep Australian/New Zealand English spelling and conventions.
        - Output plain text resume content only, no commentary.
        """;

    public async Task<Resume> TailorResumeAsync(
        Resume masterResume, JobPosting jobPosting, CancellationToken ct = default)
    {
        var userPrompt =
            $"""
            CANDIDATE MASTER RESUME:
            {masterResume.RawText}

            TARGET JOB POSTING:
            Title: {jobPosting.Title}
            Company: {jobPosting.Company}
            Location: {jobPosting.Location}
            Description:
            {jobPosting.Description}

            Produce the tailored resume now.
            """;

        var tailoredText = await _ai.CompleteAsync(SystemPrompt, userPrompt, ct);

        var tailored = new Resume
        {
            UserId = masterResume.UserId,
            FileName = $"{jobPosting.Company}-{jobPosting.Title}-tailored.txt",
            RawText = tailoredText,
            IsMaster = false,
            TailoredForJobPostingId = jobPosting.Id
        };

        _logger.LogInformation(
            "Tailored resume generated for user {UserId} / job {JobPostingId}",
            masterResume.UserId, jobPosting.Id);

        return await _resumes.UpsertAsync(tailored, ct);
    }
}
