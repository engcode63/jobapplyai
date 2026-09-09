using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace JobApplyAI.Infrastructure.Cosmos;

public class UserProfileRepository : CosmosUserPartitionedRepository<UserProfile>, IUserProfileRepository
{
    private readonly Container _container;

    public UserProfileRepository(Container container) : base(container)
    {
        _container = container;
    }

    public async Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var query = _container.GetItemLinqQueryable<UserProfile>(
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId) })
            .Where(u => u.UserId == userId);

        using var iterator = query.ToFeedIterator();
        if (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(ct);
            return page.FirstOrDefault();
        }
        return null;
    }
}

public class ResumeRepository : CosmosUserPartitionedRepository<Resume>, IResumeRepository
{
    public ResumeRepository(Container container) : base(container) { }
}

public class JobPostingRepository : CosmosUserPartitionedRepository<JobPosting>, IJobPostingRepository
{
    public JobPostingRepository(Container container) : base(container) { }
}

public class JobApplicationRepository : CosmosUserPartitionedRepository<JobApplication>, IJobApplicationRepository
{
    public JobApplicationRepository(Container container) : base(container) { }
}

public class CoverLetterRepository : CosmosUserPartitionedRepository<CoverLetter>, ICoverLetterRepository
{
    public CoverLetterRepository(Container container) : base(container) { }
}

public class InterviewSessionRepository : CosmosUserPartitionedRepository<InterviewSession>, IInterviewSessionRepository
{
    public InterviewSessionRepository(Container container) : base(container) { }
}
