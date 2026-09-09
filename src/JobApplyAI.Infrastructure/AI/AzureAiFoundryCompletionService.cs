using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using JobApplyAI.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace JobApplyAI.Infrastructure.AI;

/// <summary>
/// Talks to a chat-completion model deployed in Azure AI Foundry (Azure OpenAI). Uses Managed
/// Identity (DefaultAzureCredential) when no API key is configured - recommended for production.
/// </summary>
public class AzureAiFoundryCompletionService : IAiCompletionService
{
    private readonly ChatClient _chatClient;
    private readonly AzureAiFoundryOptions _options;
    private readonly ILogger<AzureAiFoundryCompletionService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public AzureAiFoundryCompletionService(
        IOptions<AzureAiFoundryOptions> options,
        ILogger<AzureAiFoundryCompletionService> logger)
    {
        _options = options.Value;
        _logger = logger;

        AzureOpenAIClient client = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new AzureOpenAIClient(new Uri(_options.Endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));

        _chatClient = client.GetChatClient(_options.ChatDeploymentName);

        // Azure OpenAI regularly returns 429 (rate limit) and transient 5xx responses under
        // load; retry those a few times with exponential backoff + jitter before giving up, and
        // bound the total time any single call can take so a stuck request can't hang a page
        // load or a background job indefinitely.
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(IsTransient)
                    .Handle<TimeoutRejectedException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1)
            })
            .AddTimeout(TimeSpan.FromSeconds(60))
            .Build();
    }

    private static bool IsTransient(RequestFailedException ex)
        => ex.Status == 429 || ex.Status >= 500;

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var chatOptions = new ChatCompletionOptions
        {
            Temperature = _options.Temperature,
            MaxOutputTokenCount = _options.MaxOutputTokens
        };

        try
        {
            ChatCompletion completion = await _resiliencePipeline.ExecuteAsync(
                async token =>
                {
                    var result = await _chatClient.CompleteChatAsync(messages, chatOptions, token);
                    return result.Value;
                },
                ct);

            return completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AI Foundry chat completion failed");
            throw;
        }
    }
}
