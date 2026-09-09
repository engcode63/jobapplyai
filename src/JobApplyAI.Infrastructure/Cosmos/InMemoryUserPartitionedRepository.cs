using System.Collections.Concurrent;
using System.Reflection;
using JobApplyAI.Application.Interfaces;

namespace JobApplyAI.Infrastructure.Cosmos;

/// <summary>
/// Thread-safe in-process repository used when no real Cosmos DB account is configured yet
/// (see <see cref="InfrastructureServiceCollectionExtensions"/> "demo mode" detection). Lets the
/// app run and be clicked through end-to-end before any Azure resources are provisioned. Data
/// does not persist across process restarts - this is a local/demo convenience only, never used
/// when a real <c>Cosmos:AccountEndpoint</c> is configured.
/// </summary>
public class InMemoryUserPartitionedRepository<T> : IUserPartitionedRepository<T> where T : class
{
    private static readonly PropertyInfo IdProperty = typeof(T).GetProperty("Id")!;
    private static readonly PropertyInfo UserIdProperty = typeof(T).GetProperty("UserId")!;

    private readonly ConcurrentDictionary<string, T> _store = new();

    private static string Key(string userId, string id) => $"{userId}::{id}";

    public Task<T?> GetAsync(string userId, string id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(Key(userId, id), out var item) ? item : null);

    public Task<IReadOnlyList<T>> ListByUserAsync(string userId, CancellationToken ct = default)
    {
        IReadOnlyList<T> results = _store.Values
            .Where(item => string.Equals((string?)UserIdProperty.GetValue(item), userId, StringComparison.Ordinal))
            .ToList();
        return Task.FromResult(results);
    }

    public Task<T> UpsertAsync(T item, CancellationToken ct = default)
    {
        var userId = (string)UserIdProperty.GetValue(item)!;
        var id = (string?)IdProperty.GetValue(item);
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString();
            IdProperty.SetValue(item, id);
        }

        _store[Key(userId, id)] = item;
        return Task.FromResult(item);
    }

    public Task DeleteAsync(string userId, string id, CancellationToken ct = default)
    {
        _store.TryRemove(Key(userId, id), out _);
        return Task.CompletedTask;
    }
}
