# Member self-service workspace — 20 October 2026

Scope and precedence: [ADR-014](../member-self-service-workspace.md), requested/approved by the user as a product decision. This is local implementation evidence, not tenant-rollout approval or live all-member acceptance.

## Changes

- Own-document APIs use validated tenant Member status (one acct=0 plus configured tid/nonempty oid), not DocumentPublisher role. Guests/unknown/duplicate claims fail closed even with a Publisher role or matching email domain.
- Server-derived session capability controls My documents and participates in the authorized React identity/cache boundary. The navigation says Shared with me; the catalog still fails closed.
- SQL scopes list/find/update/audit to authenticated member ownership; a member absent from the recipient directory can create an empty own draft. Cross-owner calls return 404, ACLs do not confer private-draft management, and source-bearing documents cannot mutate until owner verification is implemented.
- Recipient choices remain bounded/reviewed (currently two users). This is not an all-tenant people picker. Upload/publish/withdraw/issuance remain pending; no protected artifact or source eligibility is fabricated.

## Actual checks

| Command/check | Result and evidence mode |
|---|---|
| dotnet test tests/Gateway.IntegrationTests -c Release --no-restore | 37 passed, 0 skipped; synthetic test-assembly principals, static OIDC metadata; actual OpenAPI exported |
| dotnet test tests/Gateway.SqlTests -c Release --no-restore | 23 passed, 0 skipped; real isolated LocalDB; includes two-member independent drafts, no directory enrollment for owner, mutual find/update/audit denial |
| dotnet test tests/Gateway.UnitTests -c Release --no-restore | 101 passed, 0 skipped; existing domain/release regressions |
| npm run contracts | generated frontend DTOs from exported actual server OpenAPI |
| tsc --noEmit; vite build --outDir ../../.runtime/member-workspace/client | passed; isolated output, running host assets unchanged |
| npm test | 7 passed |
| npm run test:browser | 15 Chromium tests passed; intercepted synthetic APIs including role-free Reader creation, identity/member-capability clearance, keyboard/reflow and existing list behavior |
| dotnet publish src/Gateway.Web -c Release --no-restore -p:SkipClientBuild=true -o .runtime/member-workspace/publish | passed; replaced only this isolated package's app directory with the reviewed isolated frontend build |
| Artifact shape/private-file scan and git diff --check | passed; no spike.html, local config, keys or runtime journal in release |

183 automated tests passed in this run. No dependency versions were added/upgraded. No migration applied to the actual owner database. Same-origin package prepared, not activated or deployed. Client JS 463.82 kB / 144.69 kB gzip and CSS 19.34 kB / 5.11 kB gzip are build figures, not measured device load performance. [Synthetic mobile capture](2026-10-20-member-workspace-mobile.png).

## Tenant/live state

Read-only Graph inspection confirmed current web app optionalClaims=null and appRoleAssignmentRequired=true. Exact target/payload/prestate/cost/rollback prepared in ignored `.runtime/member-workspace/change-plan.json`: add only acct optional ID-token claim, and disable individual assignment requirement for this one single-tenant web app. No tenant changes, Graph directory-read permissions, new users/licenses, Azure resources, cost-bearing provisioning or deployment performed in this checkpoint. Existing tenant-wide openid/profile consent is unchanged.

Current web process/assets remain the prior artifact. Old sessions lack acct proof; the new code denies workspace access until configured claims and fresh sign-in. No fake membership claim or runtime test-persona switch was added. After separate approval, verify updated setup/readback and live A/Owner creation + cross-owner denial. A non-role Member login is still needed for live automatic-enrollment evidence; guest coverage here is synthetic.

G1 partial; G2 deferred/not passed; G6 not passed. No production-readiness, Office behavior, scanner Clean or download success claim. Next independent work is protected quarantine/evidence design; live uploads/publication stay disabled.
