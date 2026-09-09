# NextRoleAI

An original AI-powered job-application assistant for the **Australia & New Zealand** market.
Not a clone of any third-party product — built from scratch on Blazor Server + Azure Functions.

> **Note on naming:** the product/display name is **NextRoleAI** (see the app bar, page titles,
> and this README). The underlying solution, project files, and C# namespaces are still named
> `JobApplyAI.*` — renaming those is a purely internal, non-user-facing change and was left out of
> this pass to avoid a disruptive mass rename across every namespace/file; happy to do that
> rename too if you'd like the codebase identifiers to match exactly.

## Architecture

```
JobApplyAI.Web (Blazor Server, MudBlazor UI, Azure Container Apps)
JobApplyAI.Functions (Azure Functions isolated worker, .NET 8, HTTP API for external/mobile clients)
        |
        v
JobApplyAI.Application (CQRS-ish services: tailoring, cover letters, interview prep, tracking)
        |
        v
JobApplyAI.Infrastructure (Cosmos DB repositories, Azure AI Foundry client, resume parsing, job search adapters)
        |
        v
JobApplyAI.Core (domain entities/enums, no external dependencies)
```

Both the Blazor Web host and the Functions host register **identical** Application/Infrastructure
DI wiring, so business logic isn't duplicated — Functions exists to serve non-Blazor clients
(future mobile app, automation, etc.) against the same Cosmos DB data and AI services.

## Features implemented (v1 scaffold)

| Feature | Status |
|---|---|
| Resume upload & parsing (PDF via PdfPig, AI-assisted structuring) | ✅ working, needs live Cosmos/AI Foundry |
| AI resume tailoring per job posting | ✅ working, needs live AI Foundry |
| AI cover letter generation | ✅ working, needs live AI Foundry |
| Application tracking dashboard (Kanban-style, Cosmos DB backed) | ✅ working |
| AI mock interview prep (Q&A + feedback loop) | ✅ working, needs live AI Foundry |
| Job search/matching (AU & NZ) | ✅ working via Adzuna + Jooble aggregator APIs (free keys required); direct Seek/LinkedIn scraping intentionally not implemented — see below |
| Auto-fill browser extension | ❌ not started — separate deliverable (browser extension project, not Blazor/Functions) |
| Entra External ID authentication | ✅ wired for Blazor Web; Functions API JWT-validated; both run in anonymous "demo mode" until configured (see Local development) |
| Demo mode (no Azure resources required to try the app) | ✅ in-memory data store + placeholder AI responses, verified working end-to-end |

### Job search integration

Seek and LinkedIn are **deliberately not scraped**. Both explicitly prohibit automated/
programmatic data extraction in their Terms of Service (Seek's Website Terms of Use; LinkedIn's
User Agreement — LinkedIn has actively litigated against scrapers, e.g. the *hiQ Labs v.
LinkedIn* case). Neither offers a public search API open to third-party consumer apps; Seek's
only compliant integration path is its commercial Partner/Employer API program, which requires a
business agreement — not something this codebase can set up for you.

Instead, real (working, not stubbed) AU/NZ job search is implemented via two **legitimate,
publicly documented job aggregator APIs**, both with free developer tiers:

- **[Adzuna](https://developer.adzuna.com/)** — licenses and aggregates listings from thousands
  of employer career sites and job boards across Australia and New Zealand (`AdzunaJobSearchProvider`).
- **[Jooble](https://jooble.org/api/about)** — a second aggregator used alongside Adzuna to widen
  coverage further (`JoobleJobSearchProvider`).

Because both are aggregators, a single search already surfaces postings that originate from many
individual AU/NZ sites and employer career pages (including many that are also cross-posted to
Seek), without needing direct integration with each one. `JobSearchService` fans a search out to
every registered `IJobSearchProvider` and merges the results, tagging each posting's `Source`
field (`"adzuna"`, `"jooble"`, `"manual"`) so the UI can show provenance.

**To enable real results**, register free API keys and set:
- `Adzuna:AppId` / `Adzuna:AppKey` (from https://developer.adzuna.com/)
- `Jooble:ApiKey` (from https://jooble.org/api/about)

Until configured, each provider logs a warning and returns an empty list rather than throwing
(consistent with the rest of the app's "demo mode" fail-safe behaviour) — the manual job-entry
panel on the Job Search page remains available regardless, and `SeekJobSearchProvider` stays as a
stub ready to wire up real HTTP calls if/when Seek partner API access is obtained.

> Azure AI Foundry setup notes have moved to `notes-azure-foundry-setup.txt` in the repo root.

## What I need from you: Microsoft Entra External ID configuration

For user authentication in the Blazor Web app:

1. An **Entra External ID (CIAM) tenant** — either an existing one or a new one created via the
   Azure Portal ("Microsoft Entra External ID" resource).
2. An **app registration** in that tenant, with:
   - Redirect URI: `https://<your-web-app-hostname>/signin-oidc`
   - Front-channel logout URL: `https://<your-web-app-hostname>/signout-callback-oidc`
   - A client secret (or configure certificate-based auth if preferred)
3. The tenant's **CIAM authority URL** (`https://<tenant-name>.ciamlogin.com/`), **Tenant ID**,
   and the app registration's **Client ID** + **Client Secret**.

Populate these into the `EntraExternalId` section of `appsettings.json` (or Key Vault/App
Configuration in production).

### Enabling social sign-in (Google, Microsoft Account, LinkedIn)

Identity providers are a **tenant configuration**, not app code — the app always receives a
standard Entra-issued token regardless of which upstream provider the user picked, so none of the
steps below require touching `JobApplyAI.Web` or `JobApplyAI.Functions`.

**Microsoft Account (personal accounts)** — built-in:
1. Entra admin center → your External ID tenant → **Identity providers** → **Microsoft Account**
   → it's available by default; just add it to your user flow (next section).

**Google** — built-in social provider, requires a Google OAuth client:
1. In [Google Cloud Console](https://console.cloud.google.com/), create an OAuth 2.0 Client ID
   (Web application type). Add the redirect URI Entra gives you
   (`https://<tenant-name>.ciamlogin.com/<tenant-id>/federation/oidc/...`, shown when you add the
   provider in step 2).
2. Entra admin center → **Identity providers** → **Google** → paste the Google Client ID/Secret
   → Save.
3. Add "Google" to your user flow's identity provider list.

**LinkedIn** — not a built-in provider; add as a **Custom OpenID Connect provider**:
1. Register an app in the [LinkedIn Developer Portal](https://www.linkedin.com/developers/apps),
   request the `openid`, `profile`, `email` scopes (Sign In with LinkedIn using OpenID Connect
   product), and note the Client ID/Secret.
2. Entra admin center → **Identity providers** → **Add** → **Custom OpenID Connect provider**.
   Supply:
   - Metadata URL: `https://www.linkedin.com/oauth/.well-known/openid-configuration`
   - Client ID / Client secret from step 1
   - Scope: `openid profile email`
   - Response type: `code`
3. Add the new custom provider to your user flow's identity provider list.

**Wiring providers into sign-in — user flows:**
1. Entra admin center → your CIAM tenant → **User flows** → create (or edit) a **Sign up and
   sign in** flow.
2. Under **Identity providers**, check Email accounts / Microsoft Account / Google / your LinkedIn
   custom provider — whichever combination you want visible on the login screen.
3. Link the user flow to the app registration used by `JobApplyAI.Web` (and the API app
   registration used by `JobApplyAI.Functions`, if it also needs direct sign-in).

Once a user flow with providers is live, the sign-in screen shows all enabled buttons
automatically (Continue with Google / Continue with Microsoft / Continue with LinkedIn / email)
— no changes needed to `Program.cs` or Razor pages, since `Microsoft.Identity.Web` just redirects
to whatever Entra External ID renders for that user flow.

### Functions API token requirements

The Functions API (`JobApplyAI.Functions`) requires callers to present a valid Entra External ID
access token as a `Authorization: Bearer <token>` header on every request. Configure:

- `EntraExternalId:Authority` — the CIAM authority (used to discover signing keys/issuer).
- `EntraExternalId:Audience` — the expected `aud` claim; set this to the Client ID (or exposed
  API's Application ID URI) of whichever app registration issues tokens for this API. If the
  Blazor Web app calls the Functions API directly, expose an API scope on the Functions' app
  registration and have the Web app request an access token for that scope (on-behalf-of/
  confidential client flow) rather than reusing its own sign-in ID token.
- `EntraExternalId:RequireAuthentication` — leave `true` always; the `false` fallback exists only
  for local UI review before a CIAM tenant is provisioned, and must never be set in a deployed
  environment (the app deliberately fails closed/401 without it).

The middleware also enforces that the `{userId}` route segment on every per-user endpoint matches
the token's `oid` claim, returning `403 Forbidden` on mismatch — this prevents a valid, authenticated
user from accessing another user's data by editing the URL.

## Local development

```powershell
cd src\JobApplyAI.Web
dotnet run
```

The app runs **fully anonymous in "demo mode"** until you configure real Azure resources — no
Azure account, Cosmos DB, AI Foundry, or Entra tenant required to try it out:
- **Data storage** falls back to an in-process in-memory store (see `InMemoryUserPartitionedRepository`)
  when `Cosmos:AccountEndpoint` is left as its placeholder value. Every visitor shares one
  `demo-user` identity, and data resets whenever the process restarts.
- **AI features** (resume structuring, tailoring, cover letters, interview prep) fall back to
  `LocalDemoCompletionService`, which returns clearly-labelled placeholder text instead of a real
  model response, when `AzureAiFoundry:Endpoint`/`ChatDeploymentName` are left as placeholders.
- **Authentication** is skipped entirely (no login redirect, no OIDC handler registered) when
  `EntraExternalId:Instance`/`TenantId`/`ClientId` are left as placeholders — registering the
  OIDC middleware against an invalid placeholder authority URL would otherwise throw on every
  request, so the whole authentication pipeline is conditionally omitted instead.

This means every page (Home, Resumes, Job Search, Applications, Cover Letters, Interview Prep) is
fully clickable end-to-end immediately after `dotnet run` - upload a resume, add/search job
postings, tailor a resume, generate a cover letter, run a mock interview - all before any Azure
resource exists. As soon as you fill in real Cosmos/AI Foundry/Entra values, the app
transparently switches to the real backends with no code changes.

## Deploying infrastructure

```powershell
az group create -n rg-jobapplyai-dev -l australiaeast
az deployment group create -g rg-jobapplyai-dev -f infra\main.bicep -p infra\main.parameters.json
```

This provisions: Cosmos DB (serverless, RBAC-only, per-user partitioned containers), an Azure AI
Foundry resource + chat model deployment, a Function App (Linux Consumption, isolated worker),
a Container Apps environment + Container App for the Blazor Server host, Application Insights,
and a shared user-assigned managed identity with RBAC access to both Cosmos DB and Azure OpenAI
— no connection strings or API keys are provisioned; everything uses Managed Identity.

## Remaining/known work before this is fully "production ready"

- **Functions API auth**: ✅ **implemented.** `JwtBearerAuthenticationMiddleware` validates Entra
  External ID bearer access tokens (via OpenID Connect discovery + signing-key rotation) on every
  HTTP trigger, and each function additionally calls `AuthorizeUserAsync` to confirm the route's
  `{userId}` segment matches the token's `oid` claim — a caller can never read/write another
  user's data, even with a valid token. Configure `EntraExternalId:Authority` and
  `EntraExternalId:Audience` (see below) to activate; until then, requests are rejected with 401
  by design (fail closed, not fail open).
- **Job search provider**: implement real Seek/Indeed integration once partner API access is
  secured (see above).
- **Auto-fill browser extension**: separate project (Chrome/Edge extension, likely
  TypeScript/JS) — not part of this Blazor/Functions solution.
- **Rate limiting / cost controls** on AI endpoints (Azure OpenAI calls are billed per token).
- **CI/CD pipeline** (GitHub Actions/Azure DevOps) to build container images and deploy Bicep +
  app code — not yet created.
- **Automated tests** (unit tests for Application services, integration tests for Cosmos
  repositories) — not yet created.
