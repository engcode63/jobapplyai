using Azure.Monitor.OpenTelemetry.Exporter;
using JobApplyAI.Application;
using JobApplyAI.Functions.Security;
using JobApplyAI.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Validates Entra External ID bearer tokens on every HTTP trigger and rejects unauthenticated
// or cross-user requests before a function body runs (see Security/JwtBearerAuthenticationMiddleware.cs).
builder.UseMiddleware<JwtBearerAuthenticationMiddleware>();

// Same Cosmos DB / Azure AI Foundry wiring as the Blazor Web host, so this API can serve
// external/mobile clients against identical business logic.
builder.Services.AddJobApplyAiApplication();
builder.Services.AddJobApplyAiInfrastructure(builder.Configuration);
builder.Services.Configure<EntraExternalIdOptions>(builder.Configuration.GetSection("EntraExternalId"));

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
