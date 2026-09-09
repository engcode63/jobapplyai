using FluentAssertions;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Application.Services;
using JobApplyAI.Core.Entities;
using JobApplyAI.Core.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace JobApplyAI.Application.Tests;

[TestFixture]
public class JobSearchServiceTests
{
    private static Mock<IJobSearchProvider> CreateProvider(
        string name, IReadOnlyList<JobPosting> results)
    {
        var provider = new Mock<IJobSearchProvider>();
        provider.SetupGet(p => p.ProviderName).Returns(name);
        provider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Region>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);
        return provider;
    }

    [Test]
    public async Task SearchAsync_MergesResultsFromAllProviders()
    {
        var adzuna = CreateProvider("adzuna", new[] { new JobPosting { Title = "Electrician" } });
        var jooble = CreateProvider("jooble", new[] { new JobPosting { Title = "Fitter" } });
        var sut = new JobSearchService(
            new[] { adzuna.Object, jooble.Object }, Mock.Of<ILogger<JobSearchService>>());

        var results = await sut.SearchAsync("trades", "Sydney", Region.Australia);

        results.Should().HaveCount(2);
        results.Select(r => r.Title).Should().Contain(new[] { "Electrician", "Fitter" });
    }

    [Test]
    public async Task SearchAsync_WhenOneProviderThrows_StillReturnsResultsFromHealthyProviders()
    {
        var healthy = CreateProvider("adzuna", new[] { new JobPosting { Title = "Electrician" } });
        var broken = new Mock<IJobSearchProvider>();
        broken.SetupGet(p => p.ProviderName).Returns("jooble");
        broken
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Region>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("simulated outage"));

        var sut = new JobSearchService(
            new[] { healthy.Object, broken.Object }, Mock.Of<ILogger<JobSearchService>>());

        var results = await sut.SearchAsync("trades", "Sydney", Region.Australia);

        results.Should().ContainSingle(r => r.Title == "Electrician");
    }

    [Test]
    public async Task SearchAsync_WithNoProviders_ReturnsEmptyList()
    {
        var sut = new JobSearchService(
            Array.Empty<IJobSearchProvider>(), Mock.Of<ILogger<JobSearchService>>());

        var results = await sut.SearchAsync("trades", "Sydney", Region.NewZealand);

        results.Should().BeEmpty();
    }
}
