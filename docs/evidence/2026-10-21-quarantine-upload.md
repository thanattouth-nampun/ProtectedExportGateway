# Local quarantine receipt evidence — 21 October 2026

## Implemented and tested

210 distinct automated tests passed; no skips or new dependency versions. Tests use synthetic identities/data, and opaque test bytes are deliberately **not** workbook/protection/scanner acceptance.

| Check | Actual result |
|---|---|
| dotnet test tests/Gateway.UnitTests -c Release --no-restore | 101 passed |
| dotnet test tests/Gateway.IntegrationTests -c Release --no-restore | 45 passed; actual route/session/CSRF/streaming/config guards, synthetic test-assembly principals |
| dotnet test tests/Gateway.SqlTests -c Release --no-restore | 39 passed in final full run; one subsequently added completion-failure test passed separately (40 unique SQL tests) |
| npm test | 7 passed |
| npm run test:browser | 17 Chromium tests passed with intercepted synthetic APIs; two upload cases rerun after duplicate-submission UX correction, not double-counted |
| npm run contracts; tsc --noEmit | generated actual server DTOs and typecheck passed |
| vite build --outDir ../../.runtime/quarantine-spike/client | final JS 468.16 kB /145.97 kB gzip, CSS19.62/5.14; build metrics, not measured device load |
| dotnet publish ... -p:SkipClientBuild=true | same-origin local artifact built; reviewed client copied in; private runtime files and spike harness excluded |

Meaningful cases include private owner-scoped list/upload, header/query owner/path injection, Guest/foreign/missing member rejection, CSRF, chunked overflow, empty/oversize bytes, measured hash/idempotency conflict, concurrent duplicate reservation, stale ETag, stream interruption/retry cap, expired fencing, root ACL refusal, source eligibility DB prohibition, audit transaction rollback and object-success/SQL-failure orphan recovery. A streaming HTTP overflow initially mapped to 503 because of broad I/O handling; corrected to 413 and the test passes. Failed tests are not counted as successful acceptance.

## Actual Entra/local receipt

The real existing B session was refreshed via ordinary Entra SSO on the new local host; no test principal or intercepted API was used for this check. Created a dedicated synthetic draft `2704b5f0-be76-404d-9c9a-e1c79a05245b` (B owner, empty ACL). Selected the previously supplied `owner-only-synthetic.xlsx` through the actual file chooser and explicitly acknowledged synthetic data.

Observed SQL receipt `fe489695-899b-4e0d-9245-e809b9b7945a`: state AwaitingValidation, uploader B, 45,568 bytes, SHA-256 `24B7904375817C5AA033F6713D23ABE01BB52A79FD589138A0BE60463DA46235`, attempt1; source/scan/validation bindings null, published pointer null. Independent filesystem hashing of the one private stored object matched the received size/hash and the original fixture's pre-upload hash. Draft revision advanced to3 / ACL revision1; browser shows pending inspection and disables metadata editing/Publish.

This is **opaque encrypted-envelope receipt evidence only**. The source fixture's original RMS owner was historically the original Owner, not uploader B. This checkpoint did not inspect/rebind RMS ownership and cannot make B an owner; the deliberate mismatch remains unverified/pending and must fail future eligibility unless independently authorized ownership matches. No decryption/SDK operation, malware scan or OOXML validation ran, and no source was promoted or artifact offered. A file being present in quarantine does not mean it was accepted as a valid protected source.

Operator maintenance executed against the local spike and returned expiredCandidates=0 / orphansDeleted=0, retaining the bound pending object. The protected private-file and LocalDB tests above separately exercised nonzero recovery/cleanup and SQL fault cases. The local database backup was verified before migration; original drafts were retained.

After activating the final backend/client, reloaded the real draft and observed the same single pending version. The final client clears selection/ack after successful receipt, preventing an accidental second version from a repeat click; uncertain retries retain the original key. Direct B browser access to the original Owner's `/uploads` list returned DOCUMENT_NOT_FOUND with status404 in the problem body. Final SQL readback confirmed original Owner/A/B revisions unchanged, correct B actor for start/receipt events and zero export jobs. Re-hashing the supplied source confirmed it remained unchanged. [Final actual receipt screenshot](2026-10-21-quarantine-live-receipt.jpg).

G1 remains partial; G2 deferred/not passed; G6 not passed. Azure Blob exact-version intake/RBAC, scanner/helper, uploader-to-RMS owner binding, protected structural inspection, published-version replacement, automatic maintenance supervisor and full I01–I16 acceptance remain incomplete. T23 is a local guarded checkpoint, not a production protected-upload launch.
