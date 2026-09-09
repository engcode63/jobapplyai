using JobApplyAI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobApplyAI.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddJobApplyAiApplication(this IServiceCollection services)
    {
        services.AddScoped<ResumeTailoringService>();
        services.AddScoped<CoverLetterService>();
        services.AddScoped<InterviewPrepService>();
        services.AddScoped<ApplicationTrackingService>();
        services.AddScoped<JobSearchService>();
        return services;
    }
}
