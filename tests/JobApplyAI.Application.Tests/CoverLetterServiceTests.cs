using FluentAssertions;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Application.Services;
using JobApplyAI.Core.Entities;
using Moq;

namespace JobApplyAI.Application.Tests;

[TestFixture]
public class CoverLetterServiceTests
{
    private Mock<IAiCompletionService> _ai = null!;
    private Mock<ICoverLetterRepository> _coverLetters = null!;
    private CoverLetterService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ai = new Mock<IAiCompletionService>();
        _coverLetters = new Mock<ICoverLetterRepository>();
        _coverLetters
            .Setup(r => r.UpsertAsync(It.IsAny<CoverLetter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoverLetter c, CancellationToken _) => c);

        _sut = new CoverLetterService(_ai.Object, _coverLetters.Object);
    }

    [Test]
    public async Task GenerateAsync_ProducesACoverLetterLinkedToTheResumeAndJobPosting()
    {
        var resume = new Resume { Id = "resume-1", UserId = "user-1", RawText = "resume text" };
        var job = new JobPosting { Id = "job-1", Title = "Site Supervisor", Description = "Lead a maintenance crew" };
        _ai.Setup(a => a.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Dear Hiring Manager, ...");

        var coverLetter = await _sut.GenerateAsync(resume, job);

        coverLetter.UserId.Should().Be(resume.UserId);
        coverLetter.ResumeId.Should().Be(resume.Id);
        coverLetter.JobPostingId.Should().Be(job.Id);
        coverLetter.Content.Should().Be("Dear Hiring Manager, ...");
    }

    [Test]
    public async Task GenerateAsync_IncludesResumeAndJobDescriptionInThePromptSentToAi()
    {
        var resume = new Resume { Id = "resume-1", UserId = "user-1", RawText = "5 years welding experience" };
        var job = new JobPosting { Id = "job-1", Title = "Welder", Description = "Structural steel welding" };
        _ai.Setup(a => a.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("letter");

        await _sut.GenerateAsync(resume, job);

        _ai.Verify(a => a.CompleteAsync(
            It.IsAny<string>(),
            It.Is<string>(p => p.Contains(resume.RawText) && p.Contains(job.Description)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
