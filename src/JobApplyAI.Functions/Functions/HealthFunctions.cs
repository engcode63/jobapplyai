using System.Net;
using JobApplyAI.Infrastructure.AI;
using JobApplyAI.Infrastructure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;

namespace JobApplyAI.Functions.Functions;

/// <summary>
/// Unauthenticated liveness/readiness probe for load balancers and container orchestrators
/// (Azure Container Apps, App Gateway, uptime monitors). Always returns 200 while the process is
/// up and able to serve requests; the payload additionally reports whether Cosmos DB/Azure AI
/// Foundry are configured with real values or still running in in-memory/demo mode, purely as an
/// operational signal - it never fails the health check just because demo mode is active.
/// </summary>
public class HealthFunctions
{
    private readonly CosmosOptions _cosmosOptions;
    private readonly AzureAiFoundryOptions _aiOptions;

    public HealthFunctions(IOptions<CosmosOptions> cosmosOptions, IOptions<AzureAiFoundryOptions> aiOptions)
    {
        _cosmosOptions = cosmosOptions.Value;
        _aiOptions = aiOptions.Value;
    }

    [Function("Health_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            status = "Healthy",
            timestampUtc = DateTimeOffset.UtcNow,
            cosmosConfigured = IsConfigured(_cosmosOptions.AccountEndpoint),
            aiFoundryConfigured = IsConfigured(_aiOptions.Endpoint) && IsConfigured(_aiOptions.ChatDeploymentName)
        });
        return response;
    }

    private static bool IsConfigured(string? value)
        => !string.IsNullOrWhiteSpace(value) && !value.Contains('<') && !value.Contains('>');
}
