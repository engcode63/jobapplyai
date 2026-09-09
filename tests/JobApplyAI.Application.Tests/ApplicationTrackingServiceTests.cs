using FluentAssertions;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Application.Services;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;
using Moq;

namespace JobApplyAI.Application.Tests;

[TestFixture]
public class ApplicationTrackingServiceTests
{
    private Mock<IJobApplicationRepository> _applications = null!;
    private ApplicationTrackingService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _applications = new Mock<IJobApplicationRepository>();
        _applications
            .Setup(r => r.UpsertAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobApplication app, CancellationToken _) => app);
        _sut = new ApplicationTrackingService(_applications.Object);
    }

    [Test]
    public async Task CreateAsync_StartsInSavedStatusWithOneHistoryEntry()
    {
        var application = await _sut.CreateAsync("user-1", "job-1");

        application.UserId.Should().Be("user-1");
        application.JobPostingId.Should().Be("job-1");
        application.Status.Should().Be(ApplicationStatus.Saved);
        application.StatusHistory.Should().ContainSingle()
            .Which.Status.Should().Be(ApplicationStatus.Saved);

        _applications.Verify(r => r.UpsertAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ChangeStatusAsync_AppendsHistoryAndUpdatesStatusAndTimestamp()
    {
        var application = new JobApplication { UserId = "user-1", JobPostingId = "job-1" };
        application.StatusHistory.Add(new StatusHistoryEntry { Status = ApplicationStatus.Saved });
        var originalUpdatedUtc = application.UpdatedUtc;

        var result = await _sut.ChangeStatusAsync(application, ApplicationStatus.Applied);

        result.Status.Should().Be(ApplicationStatus.Applied);
        result.StatusHistory.Should().HaveCount(2);
        result.StatusHistory[^1].Status.Should().Be(ApplicationStatus.Applied);
        result.UpdatedUtc.Should().BeOnOrAfter(originalUpdatedUtc);
    }

    [Test]
    public async Task GetBoardAsync_DelegatesToRepositoryForTheGivenUser()
    {
        var expected = new List<JobApplication> { new() { UserId = "user-1" } };
        _applications.Setup(r => r.ListByUserAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetBoardAsync("user-1");

        result.Should().BeSameAs(expected);
    }
}
