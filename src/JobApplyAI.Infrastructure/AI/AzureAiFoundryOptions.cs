namespace JobApplyAI.Infrastructure.AI;

/// <summary>
/// Configuration for the Azure AI Foundry project / Azure OpenAI deployment used for
/// resume tailoring, cover letter generation, and interview prep.
/// </summary>
public class AzureAiFoundryOptions
{
    public const string SectionName = "AzureAiFoundry";

    /// <summary>Azure AI Foundry / Azure OpenAI resource endpoint, e.g. https://{resource}.openai.azure.com/ or the Foundry project endpoint.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Name of the deployed chat model (e.g. "gpt-4o-mini") created in the Foundry/Azure OpenAI project.</summary>
    public string ChatDeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Optional API key. Prefer leaving this empty in production and using Managed
    /// Identity/DefaultAzureCredential instead (set via Azure.Identity below).
    /// </summary>
    public string? ApiKey { get; set; }

    public float Temperature { get; set; } = 0.4f;

    public int MaxOutputTokens { get; set; } = 1500;
}
