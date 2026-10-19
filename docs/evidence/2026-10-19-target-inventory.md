# Target inventory and setup proposal — 19 October 2026

Authorized scope: read-only environment discovery and preparation of a concrete setup proposal. Exact tenant/domain/subscription/object IDs remain in ignored local files, not this public-source evidence record.

## Observed

- Initial Azure CLI session belonged to a different tenant. No directory query was issued against that tenant. A target-specific token attempt failed with AADSTS50020; used interactive tenant-specific sign-in with subscription discovery skipped. Did not sign out other accounts or grant new permissions.
- Target-specific Microsoft Graph token metadata matched the user-provided tenant. Access tokens were held in process memory, not printed, stored in repo files or passed on process command lines.
- Graph batch contained GET operations only: domains, bounded name-prefix application lookup, and relevant MIP service-principal role metadata. Each returned HTTP 200. Verified default/initial domains were observed.
- Application name-prefix lookup returned 0 candidates and no next page. This does not establish that no usable app exists under another name. Proposal creates a separate dedicated app rather than reusing unknown identities.
- Read actual Azure RMS and MIP Sync resource application/object IDs and relevant enabled application role IDs. Proposal generator pins the three reviewed resource/role ID pairs; similarly named unexpected resources reject.
- ARM GET `/subscriptions?api-version=2022-12-01` using the target tenant verified the user-named subscription's exact tenant binding and Enabled state. No subscription-default switch, billing action, resource creation or deployment was performed. Subscription target selection does not authorize costs.

## Reviewable artifacts

`scripts/inspect-owner-only-target.ps1 -TenantId <target>` performs read-only metadata inspection. It writes only selected metadata under `.runtime/owner-only-inventory/directory-metadata.json`; no credential material. The Graph batch HTTP POST is a transport for enclosed GET operations, not a directory mutation.

`scripts/prepare-owner-only-change-plan.ps1` reads the local report and generates `change-plan.json` plus exact `app-create.request.json` without API calls. The human-readable `change-request.md` beside them includes the exact target, selected subscription, proposed app, three role IDs, certificate settings, cost and rollback. Neither script provisions anything.

Proposed setup: one isolated single-tenant processor app/SP, one 14-day non-exportable local RSA-2048/SHA256 certificate/public-key registration, exactly Content.DelegatedReader + Content.DelegatedWriter + UnifiedPolicy.Tenant.Read application-role assignments. No SuperUser, Content.Writer, Graph file/directory application grants, new account/license, cloud resource or live file processing is included. These delegation grants can support acting in user contexts; they are not natively scoped to one configured owner/document by consent. Exact service-side allowlists/key custody and explicit administrator authorization are required.

Estimated new Azure resource spend for this local setup: THB 0 (0 resources); existing host/licensing assumed. M365/MIP entitlement not verified. Future Azure hosting needs a separate SKU/region/numeric cost approval and rollback.

## Supporting local correction/checks

Found a test-certificate compatibility issue while preparing the plan: PKI-valid-only store lookup can exclude self-signed Entra client certificates. Both spike paths now select by exact thumbprint without PKI chain filtering, then enforce private key, RSA >=2048 and validity period. No TLS validation or root store changed. Entra public-key registration/token validation remains required and untested.

- `dotnet test tests/Gateway.UnitTests --no-restore`: **65 passed**, including 6 ephemeral in-memory certificate cases (valid self-signed, expired, future, weak RSA, public-only, non-RSA). No certificate persisted/enrolled.
- `scripts/verify-protected-spike.ps1`: build 0 warnings/errors and **5 offline checks passed**. No MIP authentication/source inspection.
- Proposal generator tested against observed metadata; a strict-mode empty-array handling error was corrected. A successful generator explicitly returns status 0; local errors do not authorize execution.
- Full SQL/HTTP/frontend suites were not repeated: those paths did not change in this inventory step; prior full checkpoint remains separately recorded.

## Gates and next action

G0 now has target/domain/subscription and resource-role metadata evidence; app/certificate grants, labels, test identities, licensing and source fixture remain pending. G1/G2/G6 protected-source tests are NOT_RUN; no full gate passed. No tenant write, app registration, certificate enrollment, admin consent, label change, Azure provisioning/deployment or live MIP run occurred.

Next requires the user's approval of the exact local setup proposal before any of its mutation steps. After setup, verify actual object/assignment IDs and gather the owner/A/B/C/label/fixture scope before requesting the live processing experiment.
