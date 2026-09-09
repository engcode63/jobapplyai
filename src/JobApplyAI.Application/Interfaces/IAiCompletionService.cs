namespace JobApplyAI.Application.Interfaces;

/// <summary>
/// Abstraction over the AI/LLM provider (Azure AI Foundry / Azure OpenAI deployment).
/// Keeping this thin means swapping models/providers later doesn't ripple through the app.
/// </summary>
public interface IAiCompletionService
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
