# Owner draft and ACL checkpoint — 20 October 2026

Scope: T07/T22 metadata foundation after the accepted simple MIP experiment. Real web sign-in requires a separate web registration, not reuse of the processor's MIP permissions or identity. This checkpoint implements the session configuration and private Draft/ACL workspace; it does not complete protected intake, versions, publish/withdraw, issuance or download.

## Implemented behavior

- A DocumentPublisher with a reviewed same-tenant server identity mapping can create a private metadata draft. Tenant and owner object IDs come from the authenticated principal, never the JSON body.
- Only that publisher/owner can list, inspect, edit or view the draft's recent metadata audit. Other owners get 404; missing publisher role/unreviewed or foreign-tenant identity gets 403.
- Title/purpose and up to 50 distinct requester object IDs are bounded. Every requester must be in the reviewed server allowlist. The options endpoint exposes only display names/object IDs, not MIP credentials or source URLs. An empty ACL remains empty; no automatic broad grant.
- Metadata POST/PATCH bodies are limited to 16 KiB before deserialization; Kestrel's per-request body limit also covers chunked bodies. This limit does not replace the separately required future upload gate.
- SQL stores draft/ACL and document-bound audit atomically. Updates require exact quoted rowversion ETag in If-Match; missing ->428, stale ->412. Parent-row update locks plus rowversion prevent lost updates. ACL revision changes only with ACL membership changes.
- This milestone edits only empty private drafts. Any source version/published pointer/non-Draft status blocks mutation pending independent source RMS-owner verification. Creating a portal draft does not prove ownership of any file. CanPublish is always false.
- React My documents uses the actual generated OpenAPI DTOs, TanStack Query, React Hook Form and Zod. It supports create/edit, reviewed ACL choices, pagination, metadata history, errors, stale reload and identity-bound cleanup. Unconfirmed save responses block duplicate resubmission until explicit workspace reload. Upload/Publish are disabled.
- .NET owns OIDC code+PKCE, Secure/HttpOnly cookies, CSRF, tenant/oid checks and logout. An optional certificate-backed short-lived client assertion is signed on the server and bound to the configured tenant token endpoint. It uses a separate web certificate, not the MIP credential. The legacy secret configuration is mutually exclusive; no secret is needed by the proposed local setup.
- Strict unknown JSON fields now produce structured HTTP 400 in Development as well. API/auth routes remain outside SPA fallback; live release mode, readiness and protected downloads remain fail-closed.

## Endpoints available when configured

| Method/path | Behavior |
|---|---|
| GET /api/document-requester-options | Reviewed bounded identity options, publisher only |
| GET /api/my/documents?skip=0 | Own drafts, 25 per page |
| POST /api/documents | Create private draft from title/purpose/requesterObjectIds |
| GET /api/documents/{id} | Own draft metadata and ETag |
| PATCH /api/documents/{id} | If-Match + CSRF, atomic metadata/ACL update |
| GET /api/documents/{id}/audit | Last 50 document metadata events, owner scope |

The reader catalog still fails closed until publication/intake is implemented. ACL preparation cannot make an unpublished draft visible or grant RMS rights.

## Local configuration and migration

`scripts/prepare-owner-web.ps1 -ExistingHttpsThumbprint <reviewed-localhost-thumbprint>` reads already reviewed private target/identity records and writes a proposal plus `.runtime/owner-web/web.local.json`. It makes no API call, enrolls/trusts no certificate, migrates no database and refuses to replace an existing proposal. Configuration includes two reviewed Member identities from the prior target-bound readback and a local SQL connection; web client ID/thumbprint remain empty until approved setup. Private files are ignored. The current proposal was first prepared before the HTTPS trust readback and then updated without regenerating its reviewed role IDs.

After reviewing the actual local target database, apply the generated migration explicitly; the application does not auto-migrate. Example for the prepared private configuration (never substitute an unapproved cloud connection):

```powershell
$env:DOTNET_CLI_HOME = Join-Path $PWD '.runtime/dotnet'
$env:NUGET_PACKAGES = Join-Path $PWD '.runtime/nuget'
$localWebConfig = Get-Content .runtime/owner-web/web.local.json -Raw | ConvertFrom-Json
# Inspect existing DB physical path/backup before applying outside disposable tests.
dotnet ef database update --project src/Gateway.Infrastructure --connection $localWebConfig.ConnectionStrings.Gateway

# After web registration/certificate setup is approved and completed:
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:GATEWAY_LOCAL_CONFIG = (Resolve-Path .runtime/owner-web/web.local.json).Path
dotnet run --project src/Gateway.Web --urls https://localhost:7188
```

The database-update/real-login commands above are prepared instructions, not execution evidence. Only isolated LocalDB test databases were migrated in this checkpoint. GATEWAY_LOCAL_CONFIG is Development-only, loaded once; production configuration stays in operator-managed settings/secret providers. Mapping changes require restart; this list is not a directory-sync connector or evidence of file ownership. Cloud certificate loading/DP shared keys and deployment are not implemented by the local store reference.

Separate app registrations do not provide OS isolation on this local host: processes running as the same Windows user may access that user's other certificate keys and LocalDB data. Production needs the documented separate identities/service boundaries and least-privilege storage access; this local configuration does not prove that separation.

Migration `OwnerDraftAudit` adds nullable document/revision columns and document/tenant FK/indexes to the existing audit table. Existing job events remain compatible. Rollback needs a backup/review: reverting to `CorrelationAndExactWorkerIdentity` removes the new document audit bindings; it is not a production rollback authorization.

## Web setup proposal and later approval

The proposal below records the pre-execution boundary. The human subsequently approved it and setup completed: see [live setup evidence](evidence/2026-10-20-owner-web-live-setup.md) for actual tenant changes, local database migration and successful Owner Entra/session-to-SQL metadata checks. Later separately approved tenant-wide openid/profile consent enabled real A Reader/Owner-denial/local-logout checks; see [Reader runbook](reader-login-consent-runbook.md). Automatic post-logout return remains unproven.

Exact target/change/cost/rollback packet: `.runtime/owner-web/change-request.md`, `change-plan.json` and `app-create.request.json`.

Proposal: new single-tenant ProtectedExportGateway-Web-Local app/SP; localhost:7188 callback/signed-out redirect URIs; user assignment required; only LibraryReader and DocumentPublisher roles. Three assignments: original owner gets both roles; A gets LibraryReader. One separate 14-day non-exportable RSA certificate/public registration. OIDC openid/profile only, no Graph/MIP API grants, admin consent, client secret, new user/license or cloud resource. Estimated new Azure resource spend THB 0 on existing local host/LocalDB. Existing HTTPS developer certificate was read-only verified; no certificate/root changes were performed. Its trust has not been asserted.

Exact-thumbprint readback found that existing localhost certificate absent from both CurrentUser/Root and LocalMachine/Root. The updated private proposal additionally requests approval to trust only that existing public localhost certificate in CurrentUser/Root. No machine trust store or private key export. Rollback removes only the newly added trust entry, preserving its preexisting My certificate/key. No trust action has been performed; do not silently run broad developer-certificate trust/cleanup commands.

Rollback inventory must record each newly created object/assignment/key before further action, stop the web host and remove only those exact new web objects/credential/config. Preserve the MIP processor, other apps, sources and audit evidence. No proposal step was executed; the prior processor approval does not cover this new identity. Real Entra login/role claims and end-to-end authenticated SQL UI remain pending.

See evidence/2026-10-20-owner-workspace.md for actual checks and synthetic UI captures.
