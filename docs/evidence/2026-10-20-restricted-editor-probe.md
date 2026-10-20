# Single-recipient Restricted Editor probe — 20 October 2026

Subsequent correction: the user-reported Excel dialog displayed the seven-right candidate as Custom with an empty explicit Owner box. Read-only comparison with an Excel-created reference proved RMS ownership remained intact but identified different recipient rights and an explicit OWNER grant in the reference. A reference-matched new candidate is recorded in 2026-10-20-excel-reference-comparison.md; this document preserves the initial experiment and its limits.

Scope: the user supplied an owner-only synthetic XLSX, selected a same-tenant test recipient A, deferred crossed-account tests and explicitly requested trying Restricted Editor. This authorized one local protected candidate and SDK inspection, not tenant changes, license assignment, portal release or gate completion. Owner/A enabled Member records and object IDs were read from the configured tenant and retained in ignored private inventory. A currently has zero assigned licenses; no Office entitlement or successful A sign-in is claimed.

## Actual result

File SDK 1.18.124 / MSAL 4.83.3 / .NET 10 ran with the existing non-exportable certificate. Actual app-only tokens for policy and RMS were acquired with tenant metadata matching the configured tenant. Engine delegation was to the configured original owner, never to A.

The hash-pinned source was protected, had an active UDP child label under the label shown in the user's screenshot, and matched the original owner-only inspection predicate. The source required OWNER or EXPORT access before SetProtection. One protected `copy-a.xlsx` candidate was committed in a private local run root. A fresh output handler and read lock observed:

- Original label and RMS owner retained; protected state true.
- Exactly one ordinary user grant, matching A; no role grants.
- Exact observed rights: `VIEW`, `DOCEDIT`, `EDIT`, `VIEWRIGHTSDATA`, `REPLY`, `REPLYALL`, `FORWARD`.
- No `PRINT`, `EXTRACT`, `EXPORT`, `OWNER`, `EDITRIGHTSDATA` or `OBJMODEL` in that grant.
- Output SHA-256 `C74ECBDCFC32EE7806432DC9C5F8CD34FA1E307532555966C6EACFCF24D95787`; nonempty ContentId distinct from source. This does not prove independent cryptographic keys.
- Source SHA-256 remained `24B7904375817C5AA033F6713D23ABE01BB52A79FD589138A0BE60463DA46235`.

Machine-readable observations are beside this record. Exact private artifact path, label ID, identities and endpoint diagnostics remain in ignored `.runtime` metadata. No plaintext artifact, web download, SQL Ready binding, Blob publication, scanner claim or tenant mutation was made. Existing grants were not expanded.

## Compatibility corrections actually exercised

1. Initial runs rejected SDK `login.windows.net` challenges and resource terminal-slash differences before token acquisition. Accept only two known Entra hostnames, a matching tenant GUID or common discovery hint, and exact allowlisted root resources with optional terminal slash. Actual MSAL authority stays pinned to the configured tenant; result tenant must match. Different tenants, hosts, paths, queries, fragments, credentials and ports reject. RMS `https://aadrm.com` was added only after reading it on the already approved resource service principal.
2. The visible parent label differed from the source's UDP child label. An SDK discovery read established the child name/ID; those values were then pinned before issuing the candidate. No label policy was edited.
3. Redundantly applying the existing label as Standard produced PrivilegedRequiredException. The single-copy probe now changes only protection and preserves existing label assignment; it does not override privilege or strip the label. Independent output inspection confirms original label retention.

## Checks and limitations

- Build: zero warnings/errors.
- `dotnet test tests/Gateway.UnitTests --no-restore`: **91 passed** (including exact probe-rights and auth-challenge negative cases).
- `scripts/verify-protected-spike.ps1`: **8 offline preflight checks passed**.
- Final live command: exit **3**, `PARTIAL_NOT_PASSED`, `applyInspectMatched=true`, one output. Failed diagnostic attempts exited 2 and were not treated as successes.
- Source/output hashes rechecked after execution. SQL/HTTP/frontend unchanged; those suites were not rerun.

This is observed SDK apply/inspect evidence, not an Office test. It does not prove the Excel Permissions dialog calls this grant Restricted Editor, A can open/edit/save, cross-copy denial, saved-copy protection, cache behavior, G6 intake/scan or immutable Blob binding. G1 remains partial, G2/G6 NOT_RUN. The production `VIEW/DOCEDIT/EDIT` release contract is unchanged; the seven-right candidate is isolated to the single-recipient probe.

Next: open the verified local protected candidate as Owner and inspect Excel Permissions for A under Restricted Editor. This is a UI readback, not proof of A's effective client behavior. Later use isolated identities/caches for client access tests.

References: [Microsoft usage rights and permission levels](https://learn.microsoft.com/en-us/purview/rights-management-usage-rights), [Microsoft service-principal auth sample](https://github.com/Azure-Samples/MipSDK-File-Dotnet-ServicePrincipalAuth/blob/main/mip-sdk-dotnet-file-SPA/AuthDelegateImplementation.cs), [SDK PrivilegedRequiredException semantics](https://learn.microsoft.com/en-us/dotnet/api/microsoft.informationprotection.exceptions?view=mipsdk-dotnet-1.18).
