using System.Reflection;
using JobApplyAI.Application.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace JobApplyAI.Infrastructure.Cosmos;

/// <summary>
/// Generic Cosmos DB repository for entities partitioned by /userId. All PLM-style entities
/// (Resume, JobPosting, JobApplication, CoverLetter, InterviewSession) share this shape:
/// they expose "Id" and "UserId" properties.
/// </summary>
public class CosmosUserPartitionedRepository<T> : IUserPartitionedRepository<T> where T : class
{
    private readonly Container _container;
    private static readonly PropertyInfo IdProperty = typeof(T).GetProperty("Id")!;
    private static readonly PropertyInfo UserIdProperty = typeof(T).GetProperty("UserId")!;

    public CosmosUserPartitionedRepository(Container container)
    {
        _container = container;
    }

    public async Task<T?> GetAsync(string userId, string id, CancellationToken ct = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(userId), cancellationToken: ct);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<T>> ListByUserAsync(string userId, CancellationToken ct = default)
    {
        var query = _container.GetItemLinqQueryable<T>(
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId) })
            .Where(BuildUserIdPredicate(userId));

        using var iterator = query.ToFeedIterator();
        var results = new List<T>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(ct);
            results.AddRange(page);
        }
        return results;
    }

    public async Task<T> UpsertAsync(T item, CancellationToken ct = default)
    {
        var userId = (string)UserIdProperty.GetValue(item)!;
        var response = await _container.UpsertItemAsync(item, new PartitionKey(userId), cancellationToken: ct);
        return response.Resource;
    }

    public Task DeleteAsync(string userId, string id, CancellationToken ct = default)
        => _container.DeleteItemAsync<T>(id, new PartitionKey(userId), cancellationToken: ct);

    // LINQ-to-Cosmos needs a real expression tree over the entity's UserId property.
    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildUserIdPredicate(string userId)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, UserIdProperty);
        var constant = System.Linq.Expressions.Expression.Constant(userId);
        var equals = System.Linq.Expressions.Expression.Equal(property, constant);
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(equals, parameter);
    }
}
