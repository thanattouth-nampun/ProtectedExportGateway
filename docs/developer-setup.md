# Local implementation checkpoint

20 October update: [Owner Draft/ACL workspace](owner-workspace-checkpoint.md) is implemented with real local SQL tests and fixture HTTP/browser checks. Optional web certificate assertions and Development-only private configuration loading are available. Real web login is pending a dedicated web registration approval; do not reuse the MIP processor identity. Existing snippets below describe the preceding checkpoint where unchanged. The offline spike script now runs 10 checks.

Current scope is [ADR-013 standalone owner-only source](standalone-owner-only-mvp.md). External content connectors are deferred. Run `scripts/verify-protected-spike.ps1` for five offline config/hash checks; it does not authenticate or verify protected content. Live owner-only commands require the [target/permission review packet](owner-only-spike-runbook.md). Legacy unprotected-source/report tooling and runtime remain fail-closed foundations, not protected-intake acceptance.

This checkpoint provides a headless native spike, tested domain/OOXML/report foundation, .NET BFF skeleton, React HTML workspace and [durable SQL queue](sql-queue-checkpoint.md). It is **not M0 feasibility demonstrated, M1 workflow demonstrated, or a live MVP**. Authenticated lifecycle APIs, intake/worker integration and the full Owner/Reports/Jobs/Audit product flows remain to implement. Product navigation currently identifies these dependencies explicitly.

## Prerequisites and local commands

Windows x64, .NET SDK 10.0.300 (global.json), Node 24.16.0/npm 11.13.0 and SQL Server LocalDB (`SqlLocalDB` on PATH) for the SQL suite. Use `npm.cmd` in PowerShell if execution policy prevents npm.ps1. `scripts/verify.ps1` runs the full local checkpoint, including isolated SQL tests. Run from repository root:

```powershell
$env:DOTNET_CLI_HOME = Join-Path $PWD '.runtime/dotnet'
$env:NUGET_PACKAGES = Join-Path $PWD '.runtime/nuget'
dotnet restore ProtectedExportGateway.slnx --locked-mode
Push-Location src/Gateway.Client
npm.cmd ci --ignore-scripts
npm.cmd run build
npm.cmd test
Pop-Location
dotnet test tests/Gateway.UnitTests --no-restore
./scripts/verify-sql.ps1
$env:GATEWAY_EXPORT_OPENAPI = Join-Path $PWD 'src/Gateway.Web/openapi.json'
dotnet test tests/Gateway.IntegrationTests --no-restore
Push-Location src/Gateway.Client
npm.cmd run contracts
$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $PWD '../../.runtime/playwright'
.\node_modules\.bin\playwright.cmd install chromium
npm.cmd run test:browser
Pop-Location
dotnet run --project tools/MipFeasibility --no-restore -- native-check
dotnet publish src/Gateway.Web -c Release -o .runtime/publish
```

The browser installer requires package-network access; no tenant access or software-GPU launch flags. On the observed Codex host, sandbox process/loopback restrictions caused diagnostic-free MSBuild failures and loopback socket denial. Authorized restore/build/tests with normal host permissions worked; this is not an application permission requirement or a reason to run production web/worker as administrator.

Run synthetic list/detail preview:

```powershell
Push-Location src/Gateway.Client
npm.cmd run dev
# Open http://127.0.0.1:5173/spike.html locally
Pop-Location
```

`spike.html` is a separate development entry. The default release build emits only index.html and does not ship fixture metadata or persona controls. Development Vite has no auth proxy yet; product auth runs through .NET. The harness has no mutation, scan or download adapter.

Run BFF locally after client build:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/Gateway.Web --urls https://localhost:7188
```

Use a trusted developer HTTPS certificate; the HTTPS command is a run instruction, not a claim that live OIDC was tested. Without tenant settings the sign-in page works but `/auth/login` returns 503, document store readiness returns 503 and simulated downloads return 409. `/api` and `/auth` errors never receive the SPA. OpenAPI is development-only at `/openapi/v1.json`. Generated DTOs are committed; regeneration runs against the actual BFF contract exported by integration tests.

Web OIDC uses the built-in ASP.NET Core handler, code+PKCE, single-tenant issuer/tid/oid checks, a 30-minute non-sliding Secure/HttpOnly cookie and explicit antiforgery. `WebIdentity__TenantId`, `WebIdentity__ClientId`, `WebIdentity__ClientSecret` are read from environment/secret configuration; no values are included here. This temporary local confidential-client secret strategy must be replaced/reviewed with Key Vault certificate loading for Azure. LiveMip configuration explicitly fails startup in this checkpoint. No production test identity handler exists.

Local development Data Protection keys are DPAPI-protected under ignored `.runtime/web-keys`. Cloud shared keys/certificate/MI/restart continuity are unresolved; don't deploy until configured and tested. Tests use ephemeral key protection and a **test-assembly-only identity scheme**, which proves HTTP controls but not Entra authentication. Application health deliberately remains unready.

## Tool choices

xUnit 2.9.3 + Microsoft.NET.Test.Sdk 18.0.1 for domain/generator/HTTP/SQL tests; real LocalDB for transactions/constraints/concurrency; ASP.NET WebApplicationFactory for BFF integration; Vitest 5.0.5 for projection logic; Playwright 1.64.1 Chromium for browser/keyboard/session/reflow checks. No extra message broker, JS backend, substitute test DB dialect or broad component suite.

The parser is a restricted value-only synthetic-workbook profile; formulas (including internal ones), extensions, active content, unsupported parts, protected/signed packages and external relationships fail closed. ClosedXML only generates trusted synthetic data. Uploaded bytes must never reach ClosedXML. The parser library has byte/ZIP/XML/time checks but an isolated supervised parser process, full I04/I13 process tests and G6 scanner integration remain pending. Unit tests with constructed Clean evidence do not represent AV observations.

ADR-012 removes Three/R3F/Drei and T31. G7 retains frontend/session/accessibility/load/cloud acceptance; prior renderer records are historical. See [current evidence](evidence/2026-10-19-list-workspace.md).

## Next implementation dependency

The T06 storage/queue checkpoint is implemented; see sql-queue-checkpoint.md for tested boundaries. Continue T07/T22/T23 API/intake/Owner workflow and T08 bounded admission/fair scheduling/worker simulation (terminal Simulated, no artifact). Continue independent implementation while an operator completes G0/G1/G2; real adapter/release and live publishing stay gated. Stop the local Vite preview before running verification on Windows if it holds native CSS modules open; restart it afterward.
