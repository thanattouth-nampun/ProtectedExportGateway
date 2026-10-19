# Approved processor setup — 19 October 2026

The user approved the concrete setup packet after target, scope, three roles, zero new Azure resource cost and rollback were presented. This is live directory setup evidence only. Earlier target-inventory evidence describes the preceding read-only checkpoint.

## Observed result

- Created one single-tenant `ProtectedExportGateway-MipSpike` application and its service principal in the verified target tenant.
- Created one CurrentUser/My test certificate; uploaded its public certificate. Local readback: RSA 2048, SHA256WithRSA, private key present, CNG export policy None. Expiry: 2 November 2026 at 10:35:39 UTC (17:35:39 Bangkok). No private key export or root-store change.
- Created exactly three application-role assignments: RMS `Content.DelegatedReader`, RMS `Content.DelegatedWriter`, MIP Sync `UnifiedPolicy.Tenant.Read`. GET readback verified resource/principal/role IDs against the pinned allowlist, with no additional assignment or next page.
- Application GET verified single-tenant audience, one expected certificate key ID, exactly three registered application permissions, zero password credentials and zero web/SPA/public-client redirects.
- No Azure resource provisioned, license assigned, label changed or protected file processed. New Azure resource spend: THB 0; existing M365 entitlement remains unverified.

Exact object/client IDs, key ID/thumbprint, assignment IDs and sanitized API readbacks are in ignored `.runtime/owner-only-inventory/setup-*` files. The private journal is the rollback inventory. Tokens remained transient; no processor token request was made.

## Execution and recovery

The proposal's original OrderedDictionary grouping produced an empty resource application ID. Corrected to PSCustomObject records and asserted two nonempty resources and exactly three unique allowlisted roles before any mutation. The proposal generator now refuses to overwrite an existing setup journal.

`scripts/setup-owner-only-processor.ps1 -ApprovedSetupOnly` created the certificate/app/SP, then stopped with IOException before role assignment. The journal retained all three identities. After inspection, `-ResumeRecordedSetup` verified the exact live app/SP/key and existing assignments, created only missing approved assignments and completed readback successfully (exit 0). No duplicate identity or broader permissions were created. Journal writes now stage to a sibling file before replacement; the original error category remains recorded for traceability.

Supporting checks: 65 unit tests and 5 offline spike checks passed at the preceding local checkpoint; proposal payload validation, setup-script parsing, real Graph readback and local certificate metadata verification passed here. SQL/HTTP/frontend suites were not repeated because those paths did not change.

## Rollback and remaining gates

If rollback is authorized, first stop dependent processes, validate the journal tenant/app/SP bindings against live GETs, delete each recorded `/servicePrincipals/{principalId}/appRoleAssignments/{assignmentId}`, then the recorded service principal and application. Remove only the exact thumbprinted CurrentUser/My certificate/private key. Do not delete other credentials, roots or apps. Preserve sanitized observations; audit and already issued tokens/caches may persist. Rollback has not been executed.

G0 has verified target and dedicated processor setup evidence, but labels, owner/A/B/C mappings and licensing remain pending. G1 app-only MIP authentication/owner-only reprotection, G2 real Office crossed-copy/edit/save tests and G6 protected intake/scanning remain NOT_RUN. No Ready artifact or downloadable simulated substitute exists.

Next: select existing same-tenant owner/A/B/C identities, existing user-defined-permission label and an owner-only synthetic XLSX fixture. Pin the source hash and permitted SDK endpoints, review the exact local processing scope, and obtain that experiment's approval before requesting processor tokens or reading protected content.
