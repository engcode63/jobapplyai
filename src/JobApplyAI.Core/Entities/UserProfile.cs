using JobApplyAI.Core.Enums;

namespace JobApplyAI.Core.Entities;

/// <summary>
/// Represents an authenticated user (identity comes from Microsoft Entra External ID).
/// Cosmos DB partition key: /userId
/// </summary>
public class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Entra External ID object id (oid claim). Used as the Cosmos partition key.</summary>
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public Region Region { get; set; } = Region.Australia;

    public string? PhoneNumber { get; set; }

    public string? LinkedInUrl { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
