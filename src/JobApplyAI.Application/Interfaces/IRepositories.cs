using JobApplyAI.Core.Entities;

namespace JobApplyAI.Application.Interfaces;

/// <summary>Generic repository abstraction over a Cosmos DB container partitioned by userId.</summary>
public interface IUserPartitionedRepository<T> where T : class
{
    Task<T?> GetAsync(string userId, string id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListByUserAsync(string userId, CancellationToken ct = default);
    Task<T> UpsertAsync(T item, CancellationToken ct = default);
    Task DeleteAsync(string userId, string id, CancellationToken ct = default);
}

public interface IUserProfileRepository : IUserPartitionedRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct = default);
}

public interface IResumeRepository : IUserPartitionedRepository<Resume> { }

public interface IJobPostingRepository : IUserPartitionedRepository<JobPosting> { }

public interface IJobApplicationRepository : IUserPartitionedRepository<JobApplication> { }

public interface ICoverLetterRepository : IUserPartitionedRepository<CoverLetter> { }

public interface IInterviewSessionRepository : IUserPartitionedRepository<InterviewSession> { }
