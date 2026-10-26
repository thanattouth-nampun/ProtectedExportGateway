# T23 local quarantine receipt checkpoint — 21 October 2026

This is a guarded **Development-only synthetic receipt spike**, not acceptance of protected-source production intake. ADR-013/014 ownership/inspection/scanner/release gates remain. The byte envelope is untrusted even if its filename ends in XLSX. No scanner, OOXML parser, MIP operation, source promotion, eligibility or download is invoked by this path.

## Protocol and isolation

`GET /api/documents/{id}/uploads` returns the owner's latest 50 local intake metadata records. `POST` at the same path streams one raw `application/octet-stream` body, not multipart. Required headers: server-validated CSRF, exact current quoted document `If-Match`, GUID `Idempotency-Key`, URI-encoded `X-Upload-Filename` and `X-Synthetic-Fixture: true`. No query parameters or other X-Upload-* security fields are accepted. Tenant/owner come from validated single acct=0/tid/oid session; no recipients, source path, label or owner comes from the upload. The filename is bounded display metadata, never a filesystem path. Source ownership remains unknown.

Raw transport was chosen to avoid form buffering/temp files before the bounded application stream. It stays below the original 12 MiB request budget: content is 1 byte–10 MiB; Kestrel and a counting stream permit only one extra byte for deterministic overflow detection. No Content-Length is trusted for actual measurement; chunked requests are bounded too. The upload has a two-minute application cancellation deadline. Synthetic acknowledgment is an explicit scope declaration, not a content detector or security verdict.

APIs enforce Member/owner access before reading the body. Other-owner listing/upload gives 404; foreign/Guest/unknown membership is denied. Disabled local configuration returns 503, not simulated success. The session capability `canUploadToLocalQuarantine` controls React and is included in identity/cache boundaries. Backend independently enforces configuration/auth/owner/limits. No token goes to persistent browser storage.

## Durable state and failure handling

- Additive migration `20261021045922_QuarantineUpload` adds upload key, private token, lease/deadline, attempt count, measured bytes/hash and safe failure code to DocumentVersions; unique `(tenant,uploader,document,key)` prevents duplicate bindings. No source/Blob binding is invented.
- SQL transaction/app-lock serializes bounded reservations across documents. The parent row is locked, ETag checked and revision/audit advanced atomically. Uploading is durable before file receipt. Up to 20 active/pending/recent failed reservations are counted, including failures in the last 24 hours; retries use at most three attempts. The queue is local-demo bounded, not per-tenant production rate limiting.
- Fresh server GUID token/create-only part file per attempt; streaming SHA-256 and size are measured, fsync performed and final file is create-only. A fresh file read verifies size/hash before SQL binding. Each new attempt uses a new token; expired writers cannot complete after recovery/retry.
- Only completed binding transaction marks AwaitingValidation and writes the receipt audit. SQL check constraints restrict this local profile to Uploading/AwaitingValidation/ValidationFailed, require binding fields for receipt and forbid source/scan/validation evidence. This profile cannot become Eligible without a future explicitly revised implementation/schema.
- Same key/completed receipt re-measures the actual retry bytes and verifies stored bytes, returning the same version despite the original stale ETag. Different bytes or filename ->409. Parallel same-key active attempt ->409. Expired/abandoned attempts require maintenance; transient failure retries need the latest ETag and are capped at three. Content errors need a new deliberate selection/key.
- After ambiguous completion failure, never delete the final object immediately: a SQL commit may already have succeeded. Failure marking is token/state-fenced. Private orphan maintenance verifies DB references before deletion.
- New upload does not change a published pointer. Published/disabled/source-bearing legacy documents currently reject this guarded path until RMS ownership integration exists. General published-version replacement is **not** completed by T23 here.
- Filesystem object IDs, tokens, paths and lease keys never appear in response DTOs. Responses expose metadata/hash, LocalQuarantineSpike mode and CanPublish=false. No raw source/export endpoint is added. Existing simulated exports remain non-downloadable.

## Private local setup / maintenance

`scripts/prepare-local-quarantine.ps1` clones the existing ignored web configuration into `.runtime/quarantine-spike/web.local.json` and creates a fresh private objects root with protected current-user/SYSTEM ACLs. It refuses existing config/root instead of resetting permissions. No tenant call, secret enrollment or DB migration. The adapter refuses unreviewed inherited/broad ACLs and reparse ancestors/objects. Same-Windows-user processes and privileged OS users remain inside the local trust boundary; this is not an Azure RBAC/WORM/managed-identity isolation proof.

Config requires Development plus `Quarantine:EnableLocalSpike=true`, `SyntheticOnlyAcknowledged=true`, `PrivateRoot` outside webroot and the local SQL store. Production startup refuses enabling this adapter. It is default-off. No public blob/SAS, source promotion scope or Azure resource was created.

Operator maintenance only (no HTTP admin route, no auto-migration/scan):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:GATEWAY_LOCAL_CONFIG = '<absolute ignored quarantine web.local.json>'
$env:GATEWAY_LOCAL_QUARANTINE_MAINTENANCE = '1'
# From the isolated published app directory:
dotnet Gateway.Web.dll
# Clear before starting the web host:
Remove-Item Env:GATEWAY_LOCAL_QUARANTINE_MAINTENANCE
```

Maintenance examines up to 20 expired uploads and 1,000 candidate filesystem entries per invocation, rechecks token/state/SQL bindings, and deletes only orphan/failed private objects older than 24 hours. Bound AwaitingValidation envelopes are retained and continue occupying the bounded queue until the later inspection lifecycle is implemented. This operator command is not an automatic production cleanup supervisor or fairness proof. Normal console host requires no admin elevation; no AV exclusion or Office execution is involved.

## Actual local rollout and rollback

After verifying the exact GatewayOwnerLocal physical data/log paths remained inside the existing workspace SQL directory, created a COPY_ONLY/CHECKSUM backup in an ignored private backup directory and `RESTORE VERIFYONLY ... WITH CHECKSUM` succeeded. Applied only the additive fifth migration; existing Owner/A/B drafts/ACL/audit were preserved. No cloud database/resources were changed.

Published a same-origin local artifact under `.runtime/quarantine-spike/publish-final`, with separately built React assets. The private config/keys/database/backup/harness are not packaged. No tenant/consent/roles/certificate changes in this checkpoint. One local launch approval-review attempt timed out without creating a process; its permitted single retry succeeded. This was not a security rejection or security bypass.

Rollback: stop the local host, disable the local spike/use the preceding member-only artifact with the additive schema retained. Preserve quarantine data/audit and verified backup. Do not automatically run migration Down or restore the backup after new writes: either can discard new upload metadata/drafts and requires data-loss review. Cloud deployment remains unapproved.

See [actual checks and bounded receipt evidence](evidence/2026-10-21-quarantine-upload.md). G1 partial; G2 deferred/not passed; G6 not passed. Next: prove same-tenant uploader/RMS owner binding and authorized private content inspection/scanner/OOXML validation before any production intake, promotion or Publish.
