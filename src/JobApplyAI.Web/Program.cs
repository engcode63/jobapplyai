using JobApplyAI.Application;
using JobApplyAI.Infrastructure;
using JobApplyAI.Web.Components;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Domain/application/infrastructure wiring (Cosmos DB + Azure AI Foundry + parsing + job search).
builder.Services.AddJobApplyAiApplication();
builder.Services.AddJobApplyAiInfrastructure(builder.Configuration);

// Microsoft Entra External ID (CIAM) authentication. Only registered once the "EntraExternalId"
// section has real values - see README for what's required. Until then the app runs fully
// anonymous ("demo mode", see CurrentUserService's demo-user fallback) so it can be clicked
// through end-to-end before a CIAM tenant/app registration exists. Registering the OIDC handler
// against placeholder config would throw on every request (invalid authority URI), so the whole
// block is skipped rather than registered-but-broken.
var entraSection = builder.Configuration.GetSection("EntraExternalId");
var entraConfigured = IsConfigured(entraSection["Instance"]) && IsConfigured(entraSection["TenantId"])
                                                              && IsConfigured(entraSection["ClientId"]);
if (entraConfigured)
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(entraSection);
    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI();
}
else
{
    builder.Services.AddControllersWithViews();
}

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<JobApplyAI.Web.Services.CurrentUserService>();

var app = builder.Build();

if (!entraConfigured)
{
    app.Logger.LogWarning(
        "EntraExternalId is not configured - running fully anonymous in demo mode. " +
        "Every visitor shares the 'demo-user' identity/data. Configure EntraExternalId:Instance/" +
        "TenantId/ClientId (see README) before deploying for real users.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

if (entraConfigured)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Placeholder scaffold values (e.g. "<your-tenant-id>") never count as configured.
static bool IsConfigured(string? value)
    => !string.IsNullOrWhiteSpace(value) && !value.Contains('<') && !value.Contains('>');

