using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;

namespace JobApplyAI.Application.Services;

/// <summary>Drives an AI mock interview: generates the next question and, once the candidate
/// answers, generates constructive feedback grounded in the target job posting.</summary>
public class InterviewPrepService
{
    private readonly IAiCompletionService _ai;
    private readonly IInterviewSessionRepository _sessions;

    public InterviewPrepService(IAiCompletionService ai, IInterviewSessionRepository sessions)
    {
        _ai = ai;
        _sessions = sessions;
    }

    private const string QuestionSystemPrompt =
        """
        You are an interviewer for the role described. Ask exactly one realistic interview question
        (behavioural or technical) appropriate for this role, not previously asked in this session.
        Output only the question text.
        """;

    private const string FeedbackSystemPrompt =
        """
        You are an interview coach. Given the question and the candidate's answer, give concise,
        constructive feedback (2-4 sentences): what was strong, what to improve, and one tip
        using the STAR method if relevant. Output feedback only.
        """;

    public async Task<InterviewSession> StartSessionAsync(
        string userId, JobPosting jobPosting, CancellationToken ct = default)
    {
        var session = new InterviewSession { UserId = userId, JobPostingId = jobPosting.Id };
        var question = await NextQuestionAsync(jobPosting, session, ct);
        session.Exchanges.Add(new InterviewExchange { Question = question });
        return await _sessions.UpsertAsync(session, ct);
    }

    public async Task<InterviewSession> SubmitAnswerAsync(
        InterviewSession session, JobPosting jobPosting, string answer, CancellationToken ct = default)
    {
        var current = session.Exchanges[^1];
        current.UserAnswer = answer;
        current.AiFeedback = await _ai.CompleteAsync(
            FeedbackSystemPrompt,
            $"Question: {current.Question}\nCandidate answer: {answer}",
            ct);

        return await _sessions.UpsertAsync(session, ct);
    }

    public async Task<InterviewSession> NextQuestionAsync(
        InterviewSession session, JobPosting jobPosting, CancellationToken ct = default)
    {
        var question = await NextQuestionAsync(jobPosting, session, ct);
        session.Exchanges.Add(new InterviewExchange { Question = question });
        return await _sessions.UpsertAsync(session, ct);
    }

    private async Task<string> NextQuestionAsync(
        JobPosting jobPosting, InterviewSession session, CancellationToken ct)
    {
        var askedSoFar = string.Join("\n", session.Exchanges.Select(e => $"- {e.Question}"));
        var userPrompt =
            $"""
            Job title: {jobPosting.Title}
            Job description: {jobPosting.Description}
            Questions already asked:
            {askedSoFar}
            """;

        return await _ai.CompleteAsync(QuestionSystemPrompt, userPrompt, ct);
    }
}
