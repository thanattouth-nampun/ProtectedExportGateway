# Owner-only source spike — review packet, not execution authorization

Read-only target-tenant Graph/ARM metadata inspection has now been performed. No live MIP run, tenant write, certificate enrollment, admin consent, provisioning or deployment has been performed. The exact local setup proposal is in ignored `.runtime/owner-only-inventory/change-request.md` with JSON scope/payload beside it. Do not send private keys, passwords, tokens or SDK caches through chat.

## Target worksheet

| Item | Required value/status |
|---|---|
| Tenant ID and verified domain | Authenticated and read-only Graph-verified; exact IDs/domains held in ignored local target/inventory files |
| Azure subscription | User-selected name, tenant binding and Enabled state verified via ARM GET; stored only in private target metadata. Selection is not deployment/cost approval. |
| Local approved Windows x64 host and private storage root | Operator selects; private ACL/ancestor trust and temporary-data cleanup required |
| Dedicated processor app/client ID | Approved isolated app/SP created and read back; exact IDs in private journal. MIP token issuance remains untested. |
| Certificate reference in CurrentUser/My | Approved non-exportable RSA-2048 test certificate registered; expires 2 November 2026. Exact thumbprint in private target/journal. |
| Existing UDP label ID/configuration | Not verified; must permit the intended per-file rights; no global recipient changes |
| Owner and A/B/C test accounts | Same-tenant synthetic fixture identities; tid/oid-to-RMS-address binding verified out of band |
| Owner-only synthetic XLSX | Created/protected by owner through an approved existing flow; pinned encrypted SHA-256 and owner-only client baseline |
| Approved auth resources/SDK consent hosts | Exact HTTPS endpoints/resource IDs reviewed against the target, not arbitrary wildcards |
| Permission/admin-consent review | Exactly Content.DelegatedReader/Writer + UnifiedPolicy.Tenant.Read approved, assigned and read back. See evidence/2026-10-19-processor-setup.md. |
| Authorization reference | Explicit target/actions approved by the tenant owner before live run |

## Change/cost/rollback boundary

Update: the three setup actions below were approved and executed. See evidence/2026-10-19-processor-setup.md. Live file processing and Azure provisioning remain outside that approval.

The current setup proposal is one new single-tenant app/service principal, a 14-day RSA-2048/SHA256 non-exportable CurrentUser/My certificate (public key upload only), and exactly the three application roles above. No Azure resources are proposed: estimated new Azure resource cost THB 0 on the existing Windows host. Existing M365/MIP licensing remains unverified and no purchase/new test-account license is included. Application delegation permissions are not a per-owner consent boundary: the service/credential can act in user contexts supported by these grants, so code allowlists and key custody remain essential. Setup approval would not approve live processing of unspecified files/users.

Client-certificate selection now pins the operator thumbprint without requiring a public PKI chain, then explicitly checks private-key availability, RSA >=2048 and validity dates. This supports Entra's [self-signed test client certificate model](https://learn.microsoft.com/en-us/entra/identity-platform/howto-create-self-signed-certificate); it does not disable TLS validation or modify trusted roots. Unit tests use only ephemeral in-memory certificates, not certificate-store enrollment. Entra must still validate the uploaded public key during actual token issuance; that live test remains unrun.

The first proposed experiment should reuse existing approved test identities, app, label, host and certificate where possible. Running the tool requests tokens, reads policy/protected fixture content and creates two newly protected local copies; SDK service/diagnostic traffic and associated audit may occur. It does not edit tenant policy, provision Azure resources or change source permissions. A local run on existing resources proposes no new Azure resource spend; licensing/rights and any existing-host/storage/network usage must be confirmed for the actual target, not represented as universally free.

If app registration, certificate binding, permission grants/admin consent or tenant settings are missing, prepare a separate exact list of target objects and changes for approval. No automatic app creation, licensing purchase, SuperUser grant, label modification or scanner exception. Cloud VM/storage deployment needs selected subscription/region/SKU, a numeric cost estimate and its own approval; none is requested by this packet.

Rollback of approved local experiment: stop tool/native process, preserve redacted observations and hashes, verify source hash unchanged; delete only that exact private run directory after handles close and paths are verified. Review local certificate/app grant changes separately if they were approved, recording before/after IDs and removing only those changes. Revoking application permissions affects future service use; it does not recall copies already issued. RMS licenses/caches and audit records may persist. Directory removal is not secure physical erasure.

## Commands after approval and private setup

Copy `tools/MipFeasibility/owner-only.config.example.json` to a private `owner-only.local.json` outside tracked source; fill locally. Do not commit config values, private runtime files or original protected content.

```powershell
dotnet run --project tools/MipFeasibility -- owner-only-preflight <absolute-private-config>
# Only after target/actions and owner-delegation approval:
dotnet run --project tools/MipFeasibility -- owner-only-run <absolute-private-config>
```

Preflight exit 0 is only config/hash validation, not encrypted-file verification or approval. Live exit 3 is partial matching observation, never G1 PASS. Exit 2 blocks further issuance; normal errors redact raw native/MSAL details. Inspect restricted diagnostics under the approved procedure if additional evidence is needed; never paste tokens, keys or raw policy material.

## Required actual evidence before any product release

1. Original owner can open source; A/B/C cannot, on separate Office identities/caches; record exact encrypted hash/client version.
2. Prove processor identity/auth challenge/grant behavior and wrong/missing permission failures. Wrong owner, non-UDP label, non-owner source grants, expired/invalid certificate, wrong hash and stale policy must reject.
3. Independent source/output inspection: label/protection/owner/ordinary users/rights, output hashes and original unchanged. If descriptor information is unavailable, stop instead of inferring requested rights were applied.
4. A edits/saves A copy; B edits/saves B copy; owner retains access; A/B crossed-copy and C access denied, including saved copies. Confirm candidate rights meet the required behavior and do not silently substitute built-in Editor.
5. Prove protected content scan/structural checks and cleanup/process supervision (G6). Artifact candidates from this spike remain isolated and cannot be used as eligible live sources.
6. Production release later requires approved publication state, portal ACL recheck, fenced SQL transaction/audit, expiry, observed hash and exact immutable Blob version. No source URL or raw fallback.
