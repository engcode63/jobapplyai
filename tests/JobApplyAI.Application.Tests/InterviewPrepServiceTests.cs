using FluentAssertions;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Application.Services;
using JobApplyAI.Core.Entities;
using Moq;

namespace JobApplyAI.Application.Tests;

[TestFixture]
public class InterviewPrepServiceTests
{
    private Mock<IAiCompletionService> _ai = null!;
    private Mock<IInterviewSessionRepository> _sessions = null!;
    private InterviewPrepService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ai = new Mock<IAiCompletionService>();
        _sessions = new Mock<IInterviewSessionRepository>();
        _sessions
            .Setup(r => r.UpsertAsync(It.IsAny<InterviewSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InterviewSession s, CancellationToken _) => s);

        _sut = new InterviewPrepService(_ai.Object, _sessions.Object);
    }

    [Test]
    public async Task StartSessionAsync_CreatesSessionWithOneQuestionExchange()
    {
        var job = new JobPosting { Id = "job-1", Title = "Plant Operator", Description = "Operate haul trucks" };
        _ai.Setup(a => a.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tell me about a time you handled a safety incident.");

        var session = await _sut.StartSessionAsync("user-1", job);

        session.UserId.Should().Be("user-1");
        session.JobPostingId.Should().Be(job.Id);
        session.Exchanges.Should().ContainSingle();
        session.Exchanges[0].Question.Should().Be("Tell me about a time you handled a safety incident.");
        session.Exchanges[0].UserAnswer.Should().BeNull();
    }

    [Test]
    public async Task SubmitAnswerAsync_SetsAnswerAndAiFeedbackOnTheLatestExchange()
    {
        var job = new JobPosting { Id = "job-1", Title = "Plant Operator" };
        var session = new InterviewSession { UserId = "user-1", JobPostingId = job.Id };
        session.Exchanges.Add(new InterviewExchange { Question = "Describe a safety incident you handled." });

        _ai.Setup(a => a.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Good use of STAR method, be more specific about the outcome.");

        var updated = await _sut.SubmitAnswerAsync(session, job, "I stopped the line and reported it.");

        updated.Exchanges[0].UserAnswer.Should().Be("I stopped the line and reported it.");
        updated.Exchanges[0].AiFeedback.Should().Be("Good use of STAR method, be more specific about the outcome.");
    }

    [Test]
    public async Task NextQuestionAsync_AppendsANewExchangeWithoutRepeatingPreviousQuestions()
    {
        var job = new JobPosting { Id = "job-1", Title = "Plant Operator" };
        var session = new InterviewSession { UserId = "user-1", JobPostingId = job.Id };
        session.Exchanges.Add(new InterviewExchange { Question = "First question?", UserAnswer = "First answer" });

        _ai.Setup(a => a.CompleteAsync(It.IsAny<string>(), It.Is<string>(p => p.Contains("First question?")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Second question?");

        var updated = await _sut.NextQuestionAsync(session, job);

        updated.Exchanges.Should().HaveCount(2);
        updated.Exchanges[^1].Question.Should().Be("Second question?");
        updated.Exchanges[^1].UserAnswer.Should().BeNull();
    }
}
