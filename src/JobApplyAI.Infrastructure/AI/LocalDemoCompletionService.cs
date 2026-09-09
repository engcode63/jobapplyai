using Microsoft.Extensions.Logging;
using JobApplyAI.Application.Interfaces;

namespace JobApplyAI.Infrastructure.AI;

/// <summary>
/// Fallback <see cref="IAiCompletionService"/> used when no Azure AI Foundry / Azure OpenAI
/// endpoint is configured yet (see <see cref="InfrastructureServiceCollectionExtensions"/> "demo
/// mode" detection). Produces a clearly-labelled, deterministic placeholder response so every
/// AI-powered page (resume tailoring, cover letters, interview prep, resume structuring) still
/// renders something and can be clicked through end-to-end before Azure resources exist.
/// Never used once <c>AzureAiFoundry:Endpoint</c>/<c>ChatDeploymentName</c> are set.
/// </summary>
public class LocalDemoCompletionService : IAiCompletionService
{
    private readonly ILogger<LocalDemoCompletionService> _logger;

    public LocalDemoCompletionService(ILogger<LocalDemoCompletionService> logger)
    {
        _logger = logger;
    }

    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "AzureAiFoundry is not configured - returning a demo placeholder response instead of a " +
            "real AI completion. Set AzureAiFoundry:Endpoint and AzureAiFoundry:ChatDeploymentName " +
            "to enable real AI output.");

        var preview = userPrompt.Length > 400 ? userPrompt[..400] + "..." : userPrompt;

        var response =
            "[DEMO MODE - Azure AI Foundry is not configured yet, so this is placeholder text, not " +
            "a real AI-generated result. Configure AzureAiFoundry:Endpoint and ChatDeploymentName in " +
            "appsettings.json / local.settings.json to see real output.]\n\n" +
            "Here is what would have been sent to the model:\n" +
            $"- System instructions: {Truncate(systemPrompt, 200)}\n" +
            $"- Your input: {preview}\n\n" +
            "Once configured, this section will contain the actual AI-tailored resume text, cover " +
            "letter, interview question, or feedback appropriate to the feature you're using.";

        return Task.FromResult(response);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length > maxLength ? value[..maxLength] + "..." : value;
}
