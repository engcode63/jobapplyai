using Azure.Identity;
using JobApplyAI.Application.Interfaces;
using JobApplyAI.Infrastructure.AI;
using JobApplyAI.Infrastructure.Cosmos;
using JobApplyAI.Infrastructure.JobSearch;
using JobApplyAI.Infrastructure.Parsing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobApplyAI.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Cosmos DB (via Managed Identity), Azure AI Foundry chat completions,
    /// resume parsing, and job search providers. Call from both the Blazor Web host and
    /// the Azure Functions host so both share identical wiring.
    ///
    /// "Demo mode": when <c>Cosmos:AccountEndpoint</c> and/or <c>AzureAiFoundry:Endpoint</c>
    /// are left as the unfilled placeholder values from the scaffold (or blank), this method
    /// transparently swaps in an in-memory repository store and/or a placeholder-text AI
    /// service instead of failing to start. This lets the whole app be clicked through
    /// end-to-end before any Azure resources are provisioned. Real values automatically take
    /// over as soon as they're configured - no code changes needed.
    /// </summary>
    public static IServiceCollection AddJobApplyAiInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CosmosOptions>(configuration.GetSection(CosmosOptions.SectionName));
        services.Configure<AzureAiFoundryOptions>(configuration.GetSection(AzureAiFoundryOptions.SectionName));

        var cosmosOptions = configuration.GetSection(CosmosOptions.SectionName).Get<CosmosOptions>()
                            ?? new CosmosOptions();
        var aiOptions = configuration.GetSection(AzureAiFoundryOptions.SectionName).Get<AzureAiFoundryOptions>()
                         ?? new AzureAiFoundryOptions();

        if (IsConfigured(cosmosOptions.AccountEndpoint))
        {
            RegisterCosmosRepositories(services, configuration);
        }
        else
        {
            RegisterInMemoryRepositories(services);
        }

        if (IsConfigured(aiOptions.Endpoint) && IsConfigured(aiOptions.ChatDeploymentName))
        {
            services.AddSingleton<IAiCompletionService, AzureAiFoundryCompletionService>();
        }
        else
        {
            services.AddSingleton<IAiCompletionService, LocalDemoCompletionService>();
        }

        services.AddSingleton<IResumeParsingService, ResumeParsingService>();

        services.Configure<AdzunaOptions>(configuration.GetSection(AdzunaOptions.SectionName));
        services.Configure<JoobleOptions>(configuration.GetSection(JoobleOptions.SectionName));

        // Fan out job search across all legitimate providers - see SeekJobSearchProvider's
        // remarks on why Seek/LinkedIn scraping is deliberately not implemented.
        // AddStandardResilienceHandler wraps each call with a sensible default pipeline (retry
        // with jittered backoff on transient failures/5xx/429, a per-attempt timeout, a total
        // request timeout, and a circuit breaker) so a slow/flaky external job board API can't
        // hang a search or cascade into repeated failures.
        services.AddHttpClient<IJobSearchProvider, AdzunaJobSearchProvider>()
            .AddStandardResilienceHandler();
        services.AddHttpClient<IJobSearchProvider, JoobleJobSearchProvider>()
            .AddStandardResilienceHandler();
        services.AddSingleton<IJobSearchProvider, SeekJobSearchProvider>();

        return services;
    }

    /// <summary>True when a config value is present and isn't one of the scaffold's unfilled "&lt;placeholder&gt;" values.</summary>
    private static bool IsConfigured(string? value)
        => !string.IsNullOrWhiteSpace(value) && !value.Contains('<') && !value.Contains('>');

    private static void RegisterCosmosRepositories(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var options = configuration.GetSection(CosmosOptions.SectionName).Get<CosmosOptions>()
                          ?? new CosmosOptions();

            // Managed Identity in Azure; falls back through the DefaultAzureCredential chain
            // (Visual Studio/CLI login etc.) for local development.
            return new CosmosClient(options.AccountEndpoint, new DefaultAzureCredential());
        });

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var options = configuration.GetSection(CosmosOptions.SectionName).Get<CosmosOptions>()
                          ?? new CosmosOptions();
            return client.GetDatabase(options.DatabaseName);
        });

        RegisterContainer<IUserProfileRepository>(services, configuration,
            o => o.UserProfilesContainer, c => new UserProfileRepository(c));
        RegisterContainer<IResumeRepository>(services, configuration,
            o => o.ResumesContainer, c => new ResumeRepository(c));
        RegisterContainer<IJobPostingRepository>(services, configuration,
            o => o.JobPostingsContainer, c => new JobPostingRepository(c));
        RegisterContainer<IJobApplicationRepository>(services, configuration,
            o => o.JobApplicationsContainer, c => new JobApplicationRepository(c));
        RegisterContainer<ICoverLetterRepository>(services, configuration,
            o => o.CoverLettersContainer, c => new CoverLetterRepository(c));
        RegisterContainer<IInterviewSessionRepository>(services, configuration,
            o => o.InterviewSessionsContainer, c => new InterviewSessionRepository(c));
    }

    // In-memory stores must be singletons so data survives across requests within the same
    // running process (it still resets on restart - see InMemoryUserPartitionedRepository).
    private static void RegisterInMemoryRepositories(IServiceCollection services)
    {
        services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
        services.AddSingleton<IResumeRepository, InMemoryResumeRepository>();
        services.AddSingleton<IJobPostingRepository, InMemoryJobPostingRepository>();
        services.AddSingleton<IJobApplicationRepository, InMemoryJobApplicationRepository>();
        services.AddSingleton<ICoverLetterRepository, InMemoryCoverLetterRepository>();
        services.AddSingleton<IInterviewSessionRepository, InMemoryInterviewSessionRepository>();
    }

    private static void RegisterContainer<TInterface>(
        IServiceCollection services,
        IConfiguration configuration,
        Func<CosmosOptions, string> containerNameSelector,
        Func<Container, TInterface> factory)
        where TInterface : class
    {
        services.AddSingleton(sp =>
        {
            var database = sp.GetRequiredService<Database>();
            var options = configuration.GetSection(CosmosOptions.SectionName).Get<CosmosOptions>()
                          ?? new CosmosOptions();
            var container = database.GetContainer(containerNameSelector(options));
            return factory(container);
        });
    }
}

