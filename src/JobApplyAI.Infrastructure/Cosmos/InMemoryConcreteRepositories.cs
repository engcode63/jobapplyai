using JobApplyAI.Application.Interfaces;
using JobApplyAI.Core.Entities;

namespace JobApplyAI.Infrastructure.Cosmos;

// Demo-mode (no Cosmos DB configured yet) counterparts to Cosmos/ConcreteRepositories.cs.
// Same repository interfaces, backed by an in-process store instead of Cosmos DB - see
// InMemoryUserPartitionedRepository for details and lifecycle caveats.

public class InMemoryUserProfileRepository : InMemoryUserPartitionedRepository<UserProfile>, IUserProfileRepository
{
    public Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => ListByUserAsync(userId, ct).ContinueWith(t => t.Result.FirstOrDefault(), ct);
}

public class InMemoryResumeRepository : InMemoryUserPartitionedRepository<Resume>, IResumeRepository { }

public class InMemoryJobPostingRepository : InMemoryUserPartitionedRepository<JobPosting>, IJobPostingRepository { }

public class InMemoryJobApplicationRepository : InMemoryUserPartitionedRepository<JobApplication>, IJobApplicationRepository { }

public class InMemoryCoverLetterRepository : InMemoryUserPartitionedRepository<CoverLetter>, ICoverLetterRepository { }

public class InMemoryInterviewSessionRepository : InMemoryUserPartitionedRepository<InterviewSession>, IInterviewSessionRepository { }
