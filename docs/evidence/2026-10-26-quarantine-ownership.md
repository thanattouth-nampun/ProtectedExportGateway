# Quarantine ownership checkpoint — 26 October 2026

## Implementation and tests

Read-only SQL/private-envelope probe, reviewed tid/oid mapping resolution and File SDK adapter/CLI implemented. No automatic worker, schema migration, persisted ownership verdict, web release or Publish transition. Previous quarantine changes remain uncommitted alongside this work.

| Actual check | Result |
|---|---|
| Locked restore of tools/MipFeasibility, existing cache, NuGetAudit=false | Passed; offline restore is not a vulnerability audit |
| Release build, --no-restore --disable-build-servers -m:1 | Passed, zero warnings/errors |
| Unit tests Release --no-build --no-restore | 109 passed, zero skips, 8 new ownership/mapping cases |
| SQL tests Release --no-build --no-restore | 49 passed, zero skips, 9 new cases; real isolated LocalDB/files, simulated inspector |
| HTTP integration tests Release --no-restore --disable-build-servers -m:1 | 45 passed, zero skips; test principals/session/CSRF/release regression |
| scripts/verify-protected-spike.ps1 | 13 offline CLI checks passed, including acknowledgment/remote SQL/unknown-field guards; no live SDK invocation |

**203 distinct tests passed**, not live MIP/client tests. No frontend code changed or frontend tests repeated this checkpoint. Initial sandbox testhost connection timeouts are not passes; reruns outside sandbox under ordinary Windows user passed without worker/admin elevation. Initial parallel MSBuild failed without useful errors; serial builds succeeded. TRX files are in ignored test TestResults folders (ownership-unit/ownership-sql/ownership-http.trx).

Tests cover receipt/hash binding, read-only streams, missing mapping before inspection, wrong tenant/hash, tampered bytes, owner mismatch, concurrent document/version changes and inspector failure without success writes. Simulated descriptors prove decision logic only. MSAL4.84.2 aligns with existing SqlClient minimum after NU1605; FileSDK1.18.124 retained. Lockfile regenerated and locked restore passed. [Runbook/version references](../quarantine-ownership-checkpoint.md). Older issuance/Office results are not automatically revalidated.

## Actual readback

Existing tenant 9fbc4f5d-56ec-4074-82f0-69d9a86a7c06, processor 6ca99a0b-1413-4479-b9ed-8dde227f093e, label f2a92b71-fd78-41fe-bb0d-1ad3ed8dc60e and previously supplied synthetic envelope only. User approved continuing ownership milestone. No tenant/user/app/role/consent/certificate/label configuration or Azure provisioning/deployment changed. New Azure resource cost THB0; licensing not independently certified.

Target-bound Graph GET verified B (42c1e930-b22f-4a82-b7e0-daa5dd9eebdb), UPN peg-test-b@baht.net, mail null, enabled Member. UPN used as reviewed RMS candidate in private diagnostic config only; web directory/permissions unchanged. This does not prove MIP consumption entitlement.

Preflight loaded actual receipt fe489695-899b-4e0d-9245-e809b9b7945a, document 2704b5f0-be76-404d-9c9a-e1c79a05245b, uploader B: **45,568 bytes**, SHA-256 **24B7904375817C5AA033F6713D23ABE01BB52A79FD589138A0BE60463DA46235**. Private read-locked bytes matched. Result LOCAL_PREFLIGHT_ONLY_INSPECTION_NOT_RUN; no MIP initialization.

Live read-only command acquired target-tenant processor tokens for existing allowlisted policy/RMS resources, then fresh SDK handler over locked quarantine stream. Delegation stayed original owner thanattouth@baht.net, never B. Active UDP/recent policy/protected state and agreement of handler/descriptor/authorized original owner passed guards. Comparison to uploader mapping returned **RMS_OWNER_MISMATCH**, CanPublish/GatePassed=false, scan/contentValidation NOT_RUN. This actual negative ownership observation is not positive eligibility or a gate pass; rejection exit is expected.

No SetLabel/SetProtection/Commit/decrypted-content API called. End-of-probe rehash/SQL rowversion checks completed. Independent post-probe SQL confirmed AwaitingValidation/state1, original size/hash, null source/scan/validation bindings, revision3/ACLrevision1, published pointer null, original three draft/upload audit events and zero export jobs. The diagnostic writes no SQL audit; preserve evidence separately. Web still shows pending inspection, not persisted mismatch.

## Scanner readiness and limitations

Actual read-only Defender status: antivirus/realtime enabled, Normal mode, product4.18.26090.9, engine1.1.26090.9, signature1.461.206.0 at **2026-10-25 20:04:08 UTC**. This is host status, not a freshness SLA or file Clean verdict. PowerShell7 compatibility load failed due remoting support; native Windows PowerShell initially lacked CIM access inside sandbox. Same fixed status-only command outside sandbox succeeded without runas/admin-worker or setting changes. No scan/helper installation/AV exclusion/decryption/parser/cloud operation occurred.

G1 partial; G2 deferred/not passed; **G6 not passed**. Positive authenticated owner upload remains needed. T24/T25 require narrow trusted helper, supervised authorized private content inspection, linked encrypted/inspection hashes, actual trusted scan, bounded OOXML validation/cleanup, immutable Blob promotion, durable observations/fencing/UI and I01–I16 acceptance. This diagnostic is not production protected intake.
