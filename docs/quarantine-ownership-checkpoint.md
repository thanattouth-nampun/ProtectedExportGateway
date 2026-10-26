# Quarantine ownership readback checkpoint — 26 October 2026

This is a read-only operator diagnostic over the T23 local receipt, not an automatic intake worker or production eligibility path. ADR-013/014 and the existing SQL prohibition on local-source eligibility remain. No HTTP ownership assertion/override endpoint is added. Portal ownership, uploader identity and actual RMS ownership remain separate.

## Binding and execution

`SqlQuarantineOwnershipProbe` loads the exact tenant/version from SQL, requires an enabled private Draft with matching document owner/uploader, AwaitingValidation, completed receipt, no lease/source/scan/validation/published binding and the operator-pinned encrypted hash. It resolves the uploader through the existing bounded, server-reviewed identity directory. Unknown mapping fails before invoking the SDK. Browser claims, Author metadata, original display filename and supplied upload fields cannot provide an RMS mapping.

It opens only the SQL token's private `.blob` through the existing ACL/reparse guard, holds a read-only/non-write-shared handle, measures actual size/SHA-256, invokes the inspector, rehashes and rechecks both document/version SQL rowversions. Snapshot drift, changed bytes or SDK errors produce no successful report. These are diagnostic consistency checks, not a transactional production lease or publication mechanism; changes after the final reads are still possible. No DB row, audit record, intake state or web capability is modified.

The SDK adapter uses the **separately authorized original-owner delegation** from the hash-pinned private processor config. It never delegates to an uploader simply because that person uploaded a file. It confirms active UDP policy, recent policy fetch (15 minutes), protected state, actual label and fresh handler UserRights/UserRoles/owner. Handler and descriptor owners must agree and match the explicitly authorized original-owner target. The application then compares that independently read owner with the reviewed uploader mapping. Owner mismatch rejects; unknown protection/grants/role readback and non-owner grants reject. Empty explicit UserRights is only an implicit-owner candidate, not complete client acceptance. Exact strings fail closed; no guessed alias/case normalization.

Managed File SDK 1.18.124 XML/reflection confirms stream `IsProtected` and `CreateFileHandlerAsync` signatures. The managed stream overload was exercised against the real pending receipt. A synthetic server-generated `.xlsx` audit name selects the format without exposing the private token/path or trusting a user filename. No SetLabel, SetProtection, Commit, RemoveProtection, temporary decrypted-file/stream method or Office invocation is used. SDK consumption can still process private material internally; no claim of zero native memory/cache/telemetry is made.

The command has cooperative cancellation at the application seam, but the current SDK methods have no cancellation-token argument. A native hang needs an externally supervised process. This diagnostic is not the bounded production worker/scheduler/helper required by T24/T25.

## Operator use

Copy `tools/MipFeasibility/quarantine-probe.config.example.json` into ignored private runtime storage and fill exact LocalDB, private spool root, tenant/version/hash/approved label and independently reviewed identity mapping. The LocalDB-only CLI rejects remote SQL before connecting, unknown JSON fields and missing synthetic acknowledgment. Never expose these configs over HTTP. The processor auth config must have the same tenant/hash/label/authorization reference and the existing bounded owner-delegation approval/endpoint/certificate controls. Retain originals and existing private config ACLs.

```powershell
# Offline with respect to MIP/Entra; reads the local SQL receipt and private envelope only:
dotnet tools/MipFeasibility/bin/Release/net10.0/MipFeasibility.dll quarantine-owner-preflight <private-receipt-config>
# Only within the exact approved synthetic-source/original-owner processing scope:
dotnet tools/MipFeasibility/bin/Release/net10.0/MipFeasibility.dll quarantine-owner-inspect <private-receipt-config> <private-owner-auth>
```

Exit 0 means preflight or the limited owner-only observation completed; it never means a gate passed. Rejection/failure returns 2. Reports always set CanPublish/GatePassed false and scan/contentValidation NOT_RUN. Test doubles implement the same seam but are simulation, not SDK evidence. The web has not been republished in this checkpoint; its UI continues to show AwaitingValidation rather than a persisted owner verdict.

## Versions and test boundaries

Adding the SQL diagnostic dependency exposed NU1605: SqlClient 6.1.7 (already pinned through EF SQL 10.0.13) requires MSAL >=4.84.2 while the old tool directly pinned 4.83.3. Aligned to **4.84.2**, kept File SDK **1.18.124**, regenerated the tool lockfile and verified locked restore/build. Microsoft lists the package as compatible with .NET 8+ and its 4.x support range includes this version: [package/support table](https://www.nuget.org/packages/Microsoft.Identity.Client/4.84.2), [release](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/releases/tag/4.84.2). This avoids suppressing a downgrade; it is not a claim of broad live regression completion for older issuance commands.

Primary stream semantics reference: [Microsoft FileEngine API](https://microsoftdocs.github.io/mip-sdk-docs/cpp/classFileEngine.html). Installed managed signatures and actual invocation provide the managed-version evidence. The source filename extension chooses the file format; a stream is not a generic extension-independent parser.

## Actual result and next dependency

See [26 October evidence](evidence/2026-10-26-quarantine-ownership.md). The existing B-uploaded envelope produced an actual SDK `RMS_OWNER_MISMATCH` diagnostic. The original owner remains the source RMS owner; B is the uploader/portal draft owner only. No web identity directory or permissions were expanded. Positive owner-upload eligibility and the full encrypted-source scan/structure/promotion lifecycle are still unproven.

Read-only Defender status showed active Antivirus/real-time protection and engine/signature versions. That is host readiness information only, not a Clean file verdict. T24 still needs a narrowly scoped trusted helper (fixed commands, server-token/path allowlist, protected helper configuration and authenticated IPC, deadline/engine/freshness/hash binding, no worker/admin elevation); T25 needs authorized bounded private content extraction, inspection hash linked to encrypted hash, actual scan and bounded OOXML validation, cleanup/crash recovery and immutable Blob publication. Do not scan only the encrypted envelope and call it content Clean, or treat exit0 alone as evidence. No scanning, helper installation/service/IPC changes, AV exclusions, decryption or Azure provisioning occurred here.

Rollback: do not run the new operator commands; use the preceding tool artifact if required. No SQL migration/data/tenant change was made and no DB rollback is needed. Preserve source, receipt and sanitized evidence. Runtime configs, certificate and SDK cache remain ignored/private; do not delete unrelated credentials or caches. G1 remains partial; G2 deferred/not passed; G6 not passed.
