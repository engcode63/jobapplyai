using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;

namespace JobApplyAI.Application.Services;

/// <summary>Generates a tailored cover letter for a job posting using the candidate's resume.</summary>
public class CoverLetterService
{
    private readonly IAiCompletionService _ai;
    private readonly ICoverLetterRepository _coverLetters;

    public CoverLetterService(IAiCompletionService ai, ICoverLetterRepository coverLetters)
    {
        _ai = ai;
        _coverLetters = coverLetters;
    }

    private const string SystemPrompt =
        """
        You are an expert career coach writing cover letters for the Australian/New Zealand job market.
        Write a concise, professional, three-to-four paragraph cover letter.
        Only reference experience actually present in the candidate's resume - never fabricate.
        Use Australian/New Zealand English spelling and conventions. Output the letter body only.
        """;

    public async Task<CoverLetter> GenerateAsync(
        Resume resume, JobPosting jobPosting, CancellationToken ct = default)
    {
        var userPrompt =
            $"""
            CANDIDATE RESUME:
            {resume.RawText}

            JOB POSTING:
            Title: {jobPosting.Title}
            Company: {jobPosting.Company}
            Description:
            {jobPosting.Description}

            Write the cover letter now.
            """;

        var content = await _ai.CompleteAsync(SystemPrompt, userPrompt, ct);

        var coverLetter = new CoverLetter
        {
            UserId = resume.UserId,
            JobPostingId = jobPosting.Id,
            ResumeId = resume.Id,
            Content = content
        };

        return await _coverLetters.UpsertAsync(coverLetter, ct);
    }
}
