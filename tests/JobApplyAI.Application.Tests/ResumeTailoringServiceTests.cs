using FluentAssertions;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Application.Services;
using JobApplyAI.Core.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace JobApplyAI.Application.Tests;

[TestFixture]
public class ResumeTailoringServiceTests
{
    private Mock<IAiCompletionService> _ai = null!;
    private Mock<IResumeRepository> _resumes = null!;
    private ResumeTailoringService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ai = new Mock<IAiCompletionService>();
        _resumes = new Mock<IResumeRepository>();
        _resumes
            .Setup(r => r.UpsertAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resume r, CancellationToken _) => r);

        _sut = new ResumeTailoringService(_ai.Object, _resumes.Object, Mock.Of<ILogger<ResumeTailoringService>>());
    }

    [Test]
    public async Task TailorResumeAsync_SendsMasterResumeAndJobDetailsToAi()
    {
        var master = new Resume { UserId = "user-1", RawText = "10 years as a diesel fitter", IsMaster = true };
        var job = new JobPosting { Id = "job-1", Title = "Diesel Fitter", Company = "Acme Mining", Description = "Maintain haul trucks" };
        _ai.Setup(a => a.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("tailored text");

        await _sut.TailorResumeAsync(master, job);

        _ai.Verify(a => a.CompleteAsync(
            It.IsAny<string>(),
            It.Is<string>(p => p.Contains(master.RawText) && p.Contains(job.Title) && p.Contains(job.Company)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TailorResumeAsync_PersistsANewNonMasterResumeLinkedToTheJobPosting()
    {
        var master = new Resume { UserId = "user-1", RawText = "resume text", IsMaster = true };
        var job = new JobPosting { Id = "job-42", Title = "Boilermaker", Company = "Southern Steel" };
        _ai.Setup(a => a.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("tailored resume content");

        var tailored = await _sut.TailorResumeAsync(master, job);

        tailored.IsMaster.Should().BeFalse();
        tailored.UserId.Should().Be(master.UserId);
        tailored.TailoredForJobPostingId.Should().Be(job.Id);
        tailored.RawText.Should().Be("tailored resume content");
        _resumes.Verify(r => r.UpsertAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
